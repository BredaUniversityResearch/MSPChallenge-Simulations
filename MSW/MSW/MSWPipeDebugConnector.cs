using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MSW
{
	class MSWPipeDebugConnector: IDisposable
	{
		public delegate string FindServerSimulationPipeNameByEndpointDelegate(string a_watchdogToken,
			string a_simulationType);

		class PipeRequest
		{
			public string watchdog_token = null;
			public string simulation_type = null;
		};

		class PipeRequestResponse
		{
			public string pipe_name;
		};

		private NamedPipeServerStream m_debugConnectionPipe = new NamedPipeServerStream("MSWDebugConnectionPipe", PipeDirection.InOut);
		private FindServerSimulationPipeNameByEndpointDelegate m_lookupDelegate;

		public MSWPipeDebugConnector(FindServerSimulationPipeNameByEndpointDelegate a_lookupDelegate)
		{
			m_lookupDelegate = a_lookupDelegate;
			m_debugConnectionPipe.WaitForConnectionAsync().ContinueWith(OnPipeConnected);
		}

		private void OnPipeConnected(Task a_obj)
		{
			if (m_debugConnectionPipe.IsConnected)
			{
				try
				{
					PipeRequest request = null;
					using (StreamReader reader =
						new StreamReader(m_debugConnectionPipe, Encoding.UTF8, false, 128, true))
					{
						string line = reader.ReadLine();
						request = JsonConvert.DeserializeObject<PipeRequest>(line);
						Console.WriteLine($"Got Request. {line}");
					}

					if (request != null)
					{
						string responsePipeName = m_lookupDelegate(request.watchdog_token, request.simulation_type);
						PipeRequestResponse response = new PipeRequestResponse {pipe_name = responsePipeName};

						using (StreamWriter writer = new StreamWriter(m_debugConnectionPipe, Encoding.UTF8, 128, true))
						{
							string responseJson = JsonConvert.SerializeObject(response);
							writer.WriteLine(responseJson);
							writer.Flush();
							Console.WriteLine($"Wrote response. {responseJson}");
						}
					}
				}
				catch (IOException ex)
				{
					Console.WriteLine(
						$"Got debug pipe request, but IO exception occurred during communication. Ex: {ex.Message}");
				}
				catch (ObjectDisposedException ex)
				{
					Console.WriteLine($"Got Debug pipe request, but pipe was disposed... Ex: {ex.Message}");
				}
			}

			m_debugConnectionPipe.Disconnect();
			m_debugConnectionPipe.WaitForConnectionAsync().ContinueWith(OnPipeConnected);
		}

		public void Dispose()
		{
			m_debugConnectionPipe?.Dispose();
		}
	}
}
