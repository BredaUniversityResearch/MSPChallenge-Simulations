using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace MSWSupport
{
	public class CommunicationPipeHandler: IDisposable
	{
		public const string TOKEN_PRELUDE = "Token=";
		public const string MONTH_PRELUDE = "Month=";

		private NamedPipeClientStream m_communicationPipe;
		private string m_currentToken;
		private int m_currentMonth = -1;

		private Thread m_readerThread;
		private ITokenReceiver? m_tokenReceiver;
		private IUpdateMonthReceiver? m_updateMonthReceiver;

		public CommunicationPipeHandler(string targetPipeName, string simulationTypeName, string targetServer)
		{
			m_communicationPipe = new NamedPipeClientStream(".", targetPipeName, PipeDirection.In);
			Console.WriteLine("MSWPipe | Trying to connect to pipe " + targetPipeName);
			m_communicationPipe.Connect();
			Console.WriteLine("MSWPipe | Connected");

			m_readerThread = new Thread(CommunicationPipeHandlerThreadFunction);
			m_readerThread.Start(this);
		}

		public void SetTokenReceiver(ITokenReceiver tokenReceiver)
		{
			m_tokenReceiver = tokenReceiver;
		}

		public void SetUpdateMonthReceiver(IUpdateMonthReceiver updateMonthReceiver)
		{
			m_updateMonthReceiver = updateMonthReceiver;
		}

		private static void CommunicationPipeHandlerThreadFunction(object handlerObject)
		{
			CommunicationPipeHandler handler = (CommunicationPipeHandler)handlerObject;
			handler.ReadAndUpdateTokens();
		}

		private void ReadAndUpdateTokens()
		{
			using StreamReader reader = new(m_communicationPipe, Encoding.UTF8, false, 128, true);
			do
			{
				string? line = reader.ReadLine();
				if (line == null) continue;
				if (line.StartsWith(TOKEN_PRELUDE))
				{
					m_currentToken = line.Substring(line.IndexOf('=') + 1);
					Console.WriteLine("MSWPipe | Received new API token " + m_currentToken.Substring(0, 10) + "...");
					m_tokenReceiver?.UpdateAccessToken(m_currentToken);
					continue;
				}
				if (!line.StartsWith(MONTH_PRELUDE))
					continue;
				m_currentMonth = int.Parse(line.AsSpan(line.IndexOf('=') + 1));
				Console.WriteLine("MSWPipe | Received new month " + m_currentMonth + "...");
				m_updateMonthReceiver?.UpdateMonth(m_currentMonth);
			} while (!reader.EndOfStream);
		}

		public void Dispose()
		{
			m_communicationPipe.Dispose();
		}
	}
}
