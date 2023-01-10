using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace MSWSupport
{
	public interface ITokenReceiver
	{
		void UpdateAccessToken(string newAccessToken);
	};

	public class APITokenHandler: IDisposable
	{
		private const string TokenPrelude = "Token=";

		private NamedPipeClientStream m_tokenCommunicationPipe;
		private string m_currentToken = null;
		private string m_targetServer = null;

		private Thread m_readerThread;
		private ITokenReceiver m_tokenReceiver;

		public APITokenHandler(ITokenReceiver tokenReceiver, string targetPipeName, string simulationTypeName, string targetServer)
		{
			m_targetServer = targetServer;

			if (string.IsNullOrEmpty(targetPipeName))
			{
				ConsoleColor orgColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Target pipe name not specified for APITokenHandler. Did the program start from MSW?");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Attempting to connect via debug connector...");
				Console.ForegroundColor = orgColor;
				targetPipeName = MSWDebugPipeConnector.TryGetPipeForSimulationFromDebugConnection(simulationTypeName, targetServer);
				if (string.IsNullOrEmpty(targetPipeName))
				{
					return;
				}
			}

			m_tokenCommunicationPipe = new NamedPipeClientStream(".", targetPipeName, PipeDirection.In);
			Console.WriteLine("MSWPipe | Trying to connect to pipe " + targetPipeName);
			m_tokenCommunicationPipe.Connect();
			Console.WriteLine("MSWPipe | Connected");
			m_tokenReceiver = tokenReceiver;

			m_readerThread = new Thread(BackgroundReadTokens);
			m_readerThread.Start(this);
		}

		public bool CheckApiAccessWithLatestReceivedToken()
		{
			if (APIRequest.Perform(m_targetServer, "/api/Security/CheckAccess", m_currentToken, null,
				out APIAccessResult result))
			{
				return result.status != APIAccessResult.EResult.Expired;
			}

			return false;
		}

		private static void BackgroundReadTokens(object tokenHandlerObject)
		{
			APITokenHandler tokenHandler = (APITokenHandler)tokenHandlerObject;
			tokenHandler.ReadAndUpdateTokens();
		}

		private void ReadAndUpdateTokens()
		{
			if (m_tokenCommunicationPipe != null)
			{
				using (StreamReader reader = new StreamReader(m_tokenCommunicationPipe, Encoding.UTF8, false, 128, true))
				{
					do
					{
						string line = reader.ReadLine();
						if (line != null && line.StartsWith(TokenPrelude))
						{
							m_currentToken = line.Substring(line.IndexOf('=') + 1);
							Console.WriteLine("MSWPipe | Received new API token " + m_currentToken);
							m_tokenReceiver.UpdateAccessToken(m_currentToken);
						}
					} while (!reader.EndOfStream);
				}
			}
		}

		public void Dispose()
		{
			m_tokenCommunicationPipe?.Dispose();
		}
	}
}