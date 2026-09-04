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
		private string m_currentToken = string.Empty;
		private int m_currentMonth = -1;
		private readonly object m_stateLock = new();

		private Thread m_readerThread;
		private ITokenReceiver? m_tokenReceiver;
		private IUpdateMonthReceiver? m_updateMonthReceiver;

		public CommunicationPipeHandler(string targetPipeName, string simulationTypeName, string targetServer)
		{
			m_communicationPipe = new NamedPipeClientStream(".", targetPipeName, PipeDirection.In);
			ConsoleLogger.Info("MSWPipe | Trying to connect to pipe " + targetPipeName);
			m_communicationPipe.Connect();
			ConsoleLogger.Info("MSWPipe | Connected");

			m_readerThread = new Thread(CommunicationPipeHandlerThreadFunction);
			m_readerThread.Start(this);
		}

		public void SetTokenReceiver(ITokenReceiver tokenReceiver)
		{
			string tokenToReplay;
			lock (m_stateLock)
			{
				m_tokenReceiver = tokenReceiver;
				tokenToReplay = m_currentToken;
			}

			// If token arrived before receiver registration, replay it immediately.
			if (!string.IsNullOrEmpty(tokenToReplay))
			{
				tokenReceiver.UpdateAccessToken(tokenToReplay);
			}
		}

		public void SetUpdateMonthReceiver(IUpdateMonthReceiver updateMonthReceiver)
		{
			int monthToReplay;
			lock (m_stateLock)
			{
				m_updateMonthReceiver = updateMonthReceiver;
				monthToReplay = m_currentMonth;
			}

			if (monthToReplay >= 0)
			{
				updateMonthReceiver.UpdateMonth(monthToReplay);
			}
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
				string normalizedLine = line.TrimStart('\uFEFF');
				if (normalizedLine.StartsWith(TOKEN_PRELUDE))
				{
					ITokenReceiver? tokenReceiver;
					string token;
					lock (m_stateLock)
					{
						token = normalizedLine.Substring(normalizedLine.IndexOf('=') + 1);
						m_currentToken = token;
						tokenReceiver = m_tokenReceiver;
					}
					ConsoleLogger.Info("MSWPipe | Received new API token " + token.Substring(0, Math.Min(10, token.Length)) + "...");
					tokenReceiver?.UpdateAccessToken(token);
					continue;
				}
				if (!normalizedLine.StartsWith(MONTH_PRELUDE))
					continue;
				IUpdateMonthReceiver? monthReceiver;
				int month;
				lock (m_stateLock)
				{
					month = int.Parse(normalizedLine.AsSpan(normalizedLine.IndexOf('=') + 1));
					m_currentMonth = month;
					monthReceiver = m_updateMonthReceiver;
				}
				ConsoleLogger.Info("MSWPipe | Received new month " + month + "...");
				monthReceiver?.UpdateMonth(month);
			} while (!reader.EndOfStream);
		}

		public void Dispose()
		{
			m_communicationPipe.Dispose();
		}
	}
}
