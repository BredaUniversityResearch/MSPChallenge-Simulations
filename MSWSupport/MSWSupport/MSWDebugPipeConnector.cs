using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Text;
using Newtonsoft.Json;

namespace MSWSupport
{ 
	internal static class MSWDebugPipeConnector
	{
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		class PipeRequest
		{
			public string watchdog_token = null;
			public string simulation_type = null;
		};

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		class PipeRequestResponse
		{
			public string pipe_name = null;
		};

		public static string TryGetPipeForSimulationFromDebugConnection(string simulationType, string targetServer)
		{
			string watchdogToken = WatchdogTokenUtility.GetWatchdogTokenForServerAtAddress(targetServer);

			string result = null;
			using (NamedPipeClientStream stream = new NamedPipeClientStream(".", "MSWDebugConnectionPipe", PipeDirection.InOut))
			{
				try
				{
					stream.Connect(5000);
					if (stream.IsConnected)
					{
						using (StreamWriter sw = new StreamWriter(stream, Encoding.UTF8, 128, true))
						{
							PipeRequest request = new PipeRequest()
							{
								simulation_type = simulationType,
								watchdog_token = watchdogToken
							};
							sw.WriteLine(JsonConvert.SerializeObject(request));
							sw.Flush();
						}

						using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 128, false))
						{
							string response = reader.ReadLine();
							PipeRequestResponse responseObject =
								JsonConvert.DeserializeObject<PipeRequestResponse>(response);
							result = responseObject?.pipe_name;

							if (responseObject != null && string.IsNullOrEmpty(responseObject.pipe_name))
							{
								ConsoleColor orgColor = Console.ForegroundColor;
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine(
									"MSW responded with session not found or simulation not running for session, no communication can be set up.");
								Console.ForegroundColor = orgColor;
							}
						}
					}
					else
					{
						ConsoleColor orgColor = Console.ForegroundColor;
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("Failed to connect to MSW Debug pipe, no connection could be established.");
						Console.ForegroundColor = orgColor;
					}
				}
				catch (IOException ex)
				{
					Console.WriteLine(
						$"Failed to communicate with MSW via debug pipe. Exception occurred. Message: {ex.Message}");
				}
				catch (TimeoutException)
				{
					Console.WriteLine(
						$"Failed to communicate with MSW via debug pipe. Pipe did not connect.");
				}
			}

			return result;
		}
	}
}
