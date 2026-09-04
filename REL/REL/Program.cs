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
			ConsoleLogger.Info("Starting Samson Integration for MSP (REL)...");

			RiskModel model = new RiskModel();
			MswClientNotifier.Initialize();
			model.WaitForApiAccess();
			model.Run();
		}
	}
}
