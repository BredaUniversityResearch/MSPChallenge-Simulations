using System;
using System.Diagnostics;
using MSWSupport;

namespace SEL.Util
{
	public class PerformanceTimer : IDisposable
	{
		private string m_message;
		private Stopwatch m_stopWatch = new Stopwatch();

		public PerformanceTimer(string a_Message)
		{
			m_message = a_Message;
			m_stopWatch.Start();
		}

		public void Dispose()
		{
			m_stopWatch.Stop();
			ConsoleLogger.Info($"{m_stopWatch.ElapsedMilliseconds}ms".PadRight(10)+"| "+m_message);
		}
	}
}
