using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using EwEShell;
using Newtonsoft.Json.Linq;

namespace MEL
{
	/// <summary>
	/// API Connector used for debugging.
	/// This API connector write return the calculated data to disk in a known folder.
	/// </summary>
	public class ApiDebugLocalFiles: IApiConnector
	{
		private const string DebugDataFolder = "DebugData/";
		private readonly string m_ConfigFileName;

		public bool ShouldRasterizeLayers => false;
		private int m_CurrentGameMonth = -1;

		private Fishing[] m_FishingValues = null;

		public ApiDebugLocalFiles(string configFileName)
		{
			m_ConfigFileName = configFileName;
		}

		public bool CheckAPIAccess()
		{
			return true;
		}

		public int GetCurrentGameMonth(int lastSimulatedMonth)
		{
			return m_CurrentGameMonth;
		}

		public void SetInitialFishingValues(List<cScalar> fishingValues)
		{
			m_FishingValues = new Fishing[fishingValues.Count];
			for (int i = 0; i < fishingValues.Count; ++i)
			{
				m_FishingValues[i] = new Fishing { name = fishingValues[i].Name, scalar = (float)fishingValues[i].Value };
			}
		}

		public string GetMelConfigAsString()
		{
			JObject configValues = JObject.Parse(File.ReadAllText(Path.Combine(DebugDataFolder, m_ConfigFileName + ".json")));
			return configValues["MEL"].ToString();
		}

		public string[] GetUpdatedLayers()
		{
			return new string[0];
		}

		public Fishing[] GetFishingValuesForMonth(int month)
		{
			return m_FishingValues;
		}

		public void SubmitKpi(string kpiName, double kpiValue, string kpiUnits)
		{
			//Nothing
		}

		public void NotifyTickDone()
		{
			++m_CurrentGameMonth;
		}

		public double[,] GetRasterLayerByName(string layerName)
		{
			throw new System.NotImplementedException();
		}

		public void SubmitRasterLayerData(string layerName, Bitmap rasterImage)
		{
			using (Stream fs = File.OpenWrite(Path.Combine(DebugDataFolder, m_ConfigFileName,
				MEL.ConvertLayerName(layerName) + ".tif")))
			{
				rasterImage.Save(fs, ImageFormat.Png);
			}
		}

		public APILayerGeometryData GetLayerData(string layerName, int layerType, bool constructionOnly)
		{
			return null;
		}

		public double[,] GetRasterizedPressure(string name)
		{
			double[,] result = null;
			using (Stream stream = File.OpenRead(Path.Combine(DebugDataFolder, m_ConfigFileName, MEL.ConvertLayerName(name)+".tif")))
			{
				using (Bitmap bitmap = new Bitmap(stream))
				{
					result = Rasterizer.PNGToArray(bitmap, 1.0f, MEL.x_res, MEL.y_res);
				}
			}

			return result;
		}

		public void UpdateAccessToken(string newAccessToken)
		{
			//Moot. I don't think this should ever be called with a local files api.
			throw new System.NotImplementedException();
		}
	}
}
