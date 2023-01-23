using System;

namespace REL
{
	static class Program
	{
		static void Main(string[] a_args)
		{
			Console.WriteLine("Starting Samson Integration for MSP (REL)...");

			RiskModel model = new RiskModel();
			model.Run();
		}
	}
}
