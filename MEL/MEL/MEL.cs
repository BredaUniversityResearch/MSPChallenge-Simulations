using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EwEMSPLink;
using MSWSupport;
using Newtonsoft.Json;

namespace MEL
{
	/// <summary>
	/// MSP EwE Linkage, or MEL for short connects the MSP api with EwE through this adapter shell.
	/// MEL allows for the EwE simulation to be run within the MSP platform.
	///
	/// The purpose of this MEL class is to bootstrap EwE to the configured server.
	/// After this is successfully done, it will request all the required information and perform a first step.
	/// After the initial step MEL will periodically query the server through the API to see if it needs to perform another timestep
	/// </summary>
	///
	public class MEL
	{
		public const int TICK_DELAY_MS = 100;   //in ms
		private const int NEXT_RASTER_LOAD_WAITING_TIME_SEC = 10;
		private const int MAX_RASTER_LOAD_ATTEMPTS = 20;

		private static string ApiBaseURL = "http://localhost/1/"; //Default to localhost.

		private int lastupdatedmonth = -2;

		//cache layers
		private List<RasterizedLayer> layers = new List<RasterizedLayer>();

		public Dictionary<string, PressureLayer> pressureLayers = new Dictionary<string, PressureLayer>();
		public Config config;
		private string configstring;

		public float x_min;
		public float x_max;
		public float y_min;
		public float y_max;

		public static int x_res;
		public static int y_res;

		private List<Task> backgroundTasks = new List<Task>();

		private cEwEMSPLink shell;
		private List<cPressure> pressures = new List<cPressure>();
		private List<cPressure> cfishingpressures = new List<cPressure>();
		public List<cGrid> outputs = new List<cGrid>();

		private APITokenHandler tokenHandler;

		private string dumpDir;
		private UInt32 nextDumpNo = 1;

		public IApiConnector ApiConnector
		{
			get;
			private set;
		}

		public MEL()
		{
			/* Create a ConsoleTraceListener and add it to the trace listeners. */
			var myWriter = new ConsoleTraceListener();
			Trace.Listeners.Add(myWriter); // this will output all writes to "Debug."

			string? eweDumpEnabledEnvVar = Environment.GetEnvironmentVariable("MSP_MEL_EWE_DUMP_ENABLED");
			if ((CommandLineArguments.HasOptionValue("EWEDumpEnabled") && CommandLineArguments.GetOptionValue("EWEDumpEnabled") == "1") ||
				(eweDumpEnabledEnvVar is "1"))
			{
				DateTime currentDateTime = DateTime.Now;
				dumpDir = Directory.GetCurrentDirectory() + "\\mel-ewe-dump\\" + currentDateTime.ToString("yyyyMMddHHmmss") + "\\";
				Directory.CreateDirectory(dumpDir);
			}

			if (CommandLineArguments.HasOptionValue("APIEndpoint"))
			{
				ApiBaseURL = CommandLineArguments.GetOptionValue("APIEndpoint");
				ConsoleLogger.Info($"Using APIEndpoint {ApiBaseURL}");
			}
			else
			{
				ConsoleLogger.Info($"No commandline argument found for APIEndpoint. Using default value {ApiBaseURL}");
			}

			ApiConnector = new ApiMspServer(ApiBaseURL);
			//ApiConnector = new ApiDebugLocalFiles("BS_Basic");

			shell = new cEwEMSPLink();

			tokenHandler = new APITokenHandler(ApiConnector,
				CommandLineArguments.GetOptionValue(CommandLineArguments.MSWPipeName), "MEL", ApiBaseURL);

			WaitForAPIAccess();

			LoadConfig();

			x_min = config.x_min;
			y_min = config.y_min;

			x_max = config.x_max;
			y_max = config.y_max;

			InitPressureLayers();
			int attempt = 1;
			while (attempt <= MAX_RASTER_LOAD_ATTEMPTS)
			{
				ConsoleLogger.Info("Start loading pressure layers");
				LoadPressureLayers();
				WaitForAllBackgroundTasks();
				if (AreAllPressureLayersLoaded())
				{
					break;
				}
				ConsoleLogger.Error($"Found unloaded pressure layers, retrying in {NEXT_RASTER_LOAD_WAITING_TIME_SEC} sec, attempt: {attempt} of {MAX_RASTER_LOAD_ATTEMPTS}");
				Thread.Sleep(TimeSpan.FromSeconds(NEXT_RASTER_LOAD_WAITING_TIME_SEC));
				++attempt;
			}

			RasterizeLayers();

			UpdateFishing();

			WaitForAllBackgroundTasks();

			//Start values for fishing intensity as returned by EwEShell.
			List<cScalar> initialFishingValues = new List<cScalar>();

			if (shell.Configuration(configstring, initialFishingValues))
			{
				foreach (cScalar fish in initialFishingValues)
				{
					ConsoleLogger.Info($"Initialized fishing values for {fish.Name} to {fish.Value}");

					pressures.Add(new cPressure(fish.Name, fish.Value));
					cfishingpressures.Add(new cPressure(fish.Name, fish.Value));
				}
				ApiConnector.SetInitialFishingValues(initialFishingValues);

				// Dump game version for testing purposes
				ConsoleLogger.Info($"Loaded EwE model '{shell.CurrentGame.Version}', {shell.CurrentGame.Author}, {shell.CurrentGame.Contact}");

				//eweshell initialised fine
				shell.Startup();

				ConsoleLogger.Info("Startup done");
			}
			else
			{
				//something went wrong here
				ConsoleColor orgColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleLogger.Error("EwE Startup failed");
				Console.ForegroundColor = orgColor;
			}
		}

		/// <summary>
		/// load the config file from the server
		/// </summary>
		private void LoadConfig()
		{
			//file name should probably be obtained from the server
			configstring = ApiConnector.GetMelConfigAsString();

			config = JsonConvert.DeserializeObject<Config>(configstring);

			x_res = config.columns;
			y_res = config.rows;

			foreach (Outcome o in config.outcomes)
			{
				outputs.Add(new cGrid(o.name, x_res, y_res));
			}
		}

		/// <summary>
		/// Initialise the pressure layers by loading in the WKT from the server
		/// </summary>
		public void InitPressureLayers()
		{
			foreach (Pressure pressure in config.pressures)
			{
				pressureLayers[pressure.name] = new PressureLayer(pressure.name);

				foreach (LayerData layerData in pressure.layers)
				{
					if (layerData.influence > 0.0f)
					{
						RasterizedLayer rasterizedLayer = FindCachedLayerForData(layerData);
						if (rasterizedLayer == null)
						{
							rasterizedLayer = new RasterizedLayer(layerData);
							layers.Add(rasterizedLayer);
						}
						pressureLayers[pressure.name].Add(rasterizedLayer, layerData.influence);
					}
				}
			}
		}

		public bool AreAllPressureLayersLoaded()
		{
			return -1 == layers.FindIndex(x => !x.IsLoadedCorrectly);
		}

		public void LoadPressureLayers()
		{
			foreach (RasterizedLayer rasterizedLayer in layers)
			{
				if (rasterizedLayer.IsLoadedCorrectly)
				{
					continue;
				}
				AddBackgroundTask(() =>
				{
					LoadThreaded(rasterizedLayer);
				});
			}
		}

		private RasterizedLayer FindCachedLayerForData(LayerData layerData)
		{
			return layers.Find(obj => obj.constructionOnly == layerData.construction && obj.name == layerData.layer_name && obj.LayerType == layerData.layer_type);
		}

		/// <summary>
		/// Retrieve the WKT of a single layer from the server
		/// </summary>
		/// <param name="rasterizedLayer">Layer object to be loaded</param>
		private void LoadThreaded(RasterizedLayer rasterizedLayer)
		{
			rasterizedLayer.GetLayerDataAndRasterize(this);
		}

		private void WaitForAPIAccess()
		{
			bool firstAttemptFailed = true;
			while (!ApiConnector.CheckAPIAccess())
			{
				if (firstAttemptFailed)
				{
					ConsoleLogger.Error("API refused current access. Waiting for a little while and silently trying again.");
					firstAttemptFailed = false;
				}

				Thread.Sleep(100);
			}
		}

		/// <summary>
		/// Update tick for MEL, runs once per second
		/// </summary>
		public void Tick()
		{
			WaitForAPIAccess();

			var watch = Stopwatch.StartNew();
			int currentGameMonth = ApiConnector.GetCurrentGameMonth(lastupdatedmonth);
			if (currentGameMonth == -100)
			{
				return;
			}

			lastupdatedmonth = currentGameMonth;

			ConsoleLogger.Info($"Executing month: {lastupdatedmonth}");

			WaitForAllBackgroundTasks();

			//update pressure layers where needed
			UpdatePressureLayers();

			WaitForAllBackgroundTasks();

			UpdateFishing();
			RasterizeLayers();

			//Start EwE tick
			shell.Tick(pressures, outputs);
			if (dumpDir != null)
			{
				DateTime currentDateTime = DateTime.Now;
				File.WriteAllText(dumpDir + currentDateTime.ToString("yyyyMMddHHmmss") + "pressures" + nextDumpNo + ".log", JsonConvert.SerializeObject(pressures));
				File.WriteAllText(dumpDir + currentDateTime.ToString("yyyyMMddHHmmss") + "outputs" + nextDumpNo + ".log", JsonConvert.SerializeObject(outputs));
				nextDumpNo++;
			}

			StoreTick();
			SubmitCurrentKPIValues(lastupdatedmonth);

			WaitForAllBackgroundTasks();
			TickDone();

			watch.Stop();
			ConsoleLogger.Info($"Month {lastupdatedmonth} executed in: {watch.ElapsedMilliseconds}ms");

			ConsoleLogger.Info("------------------");
		}

		/// <summary>
		/// Query the server to check for updates on any layers, then load the new WKT
		/// </summary>
		private void UpdatePressureLayers()
		{
			//get the list of layers that need to be updated
			string[] toUpdate = ApiConnector.GetUpdatedLayers();
			if (toUpdate.Length == 0 || (toUpdate.Length == 1 && toUpdate[0] == ""))
			{
				return;
			}

			List<RasterizedLayer> updated = new List<RasterizedLayer>(toUpdate.Length);

			foreach (KeyValuePair<string, PressureLayer> pressure in pressureLayers)
			{
				foreach (PressureLayer.LayerEntry layerEntry in pressure.Value.GetLayerEntries())
				{
					if (layerEntry == null) continue;

					foreach (string baseName in toUpdate)
					{
						if (layerEntry.RasterizedLayer.name.Contains(baseName))
						{
							//tag the pressure layer to be redrawn
							pressure.Value.redraw = true;

							if (!updated.Contains(layerEntry.RasterizedLayer))
							{
								updated.Add(layerEntry.RasterizedLayer);
								//layer has changed, update it
								AddBackgroundTask(() => LoadThreaded(layerEntry.RasterizedLayer));
							}
						}
					}
				}
			}
		}

		private void UpdateFishing()
		{
			Fishing[] fishing = ApiConnector.GetFishingValuesForMonth(lastupdatedmonth);
			for (int i = 0; i < cfishingpressures.Count; i++)
			{
				foreach (Fishing f in fishing)
				{
					if (cfishingpressures[i].Name == f.name)
					{
						ConsoleLogger.Info($"Updated fishing values for {f.name} to {f.scalar}");
						cfishingpressures[i] = new cPressure(f.name, f.scalar);
					}
				}
			}
		}

		private void SubmitCurrentKPIValues(int currentMonth)
		{
			foreach (cGrid outcome in outputs)
			{
				ApiConnector.SubmitKpi(outcome.Name, currentMonth, outcome.Mean, outcome.Units);
			}
		}

		private void TickDone()
		{
			ApiConnector.NotifyTickDone();
		}

		private void StoreTick()
		{
			foreach (cGrid grid in outputs)
			{
				SubmitBitmapForStorage(grid);
			}
		}

		private void SubmitBitmapForStorage(cGrid grid)
		{
			using (Bitmap bitmap = Rasterizer.ToBitmapSlow(grid.Cell))
			{
				ApiConnector.SubmitRasterLayerData(grid.Name, bitmap);
			}
		}

		/// <summary>
		/// rasterize the loaded layers to .png files
		/// </summary>
		private void RasterizeLayers()
		{
			//var watch = System.Diagnostics.Stopwatch.StartNew();
			pressures.Clear();

			foreach (KeyValuePair<string, PressureLayer> entry in pressureLayers)
			{
				if (entry.Value.redraw)
					entry.Value.RasterizeLayers(this);

				pressures.Add(entry.Value.pressure);
			}

			foreach (cPressure fishing in cfishingpressures)
			{
				pressures.Add(new cPressure(fishing.Name, fishing.Scalar));
			}

			//watch.Stop();
			//Console.WriteLine("RasterizeLayers: " + watch.ElapsedMilliseconds);
		}

		private void AddBackgroundTask(Action task)
		{
			Task t = new Task(task);
			t.Start();
			backgroundTasks.Add(t);
		}

		private void WaitForAllBackgroundTasks()
		{
			while (backgroundTasks.Count > 0)
			{
				backgroundTasks[0].Wait();
				backgroundTasks.RemoveAt(0);
			}
		}

		public static string ConvertLayerName(string name)
		{
			return "mel_" + name.Replace(' ', '_');
		}
	}
}
