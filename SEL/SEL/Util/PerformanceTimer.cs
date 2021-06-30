using System;
using System.Diagnostics;

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
			Console.WriteLine(string.Format("{0}ms\t| {1} ", m_stopWatch.ElapsedMilliseconds, m_message));
		}
	}
}
