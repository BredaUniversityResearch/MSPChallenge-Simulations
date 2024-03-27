using System;
using MSWSupport;

namespace REL
{
	static class Program
	{
		static void Main(string[] a_args)
		{
	        ConsoleTextWriter.Instance.SetMessageFormat("{prefix}{message}");
	        ConsoleTextWriter.Instance.SetMessageParameter("prefix", "REL: ");
			Console.SetOut(ConsoleTextWriter.Instance);
			Console.WriteLine("Starting Samson Integration for MSP (REL)...");

			RiskModel model = new RiskModel();
			model.WaitForApiAccess();
			model.Run();
		}
	}
}
