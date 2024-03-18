using System;
using System.IO;
using System.Text;
using System.Threading;
using MSWSupport;
namespace SEL
{
	class Program
	{
		private const int TICKRATE = 500; //ms

		static void Main(string[] args)
		{
	        ConsoleTextWriter.Instance.SetMessageFormat("{prefix}{message}");
	        ConsoleTextWriter.Instance.SetMessageParameter("prefix", "SEL: ");
			Console.SetOut(ConsoleTextWriter.Instance);
			Console.WriteLine("Starting MSP2050 Shipping EmuLation version {0}", typeof(Program).Assembly.GetName().Version);

			ShippingModel model = new ShippingModel();
			model.WaitForApiAccess();
			while (true)
			{
				if (SELConfig.Instance.ShouldIgnoreApiSecurity())
				{
					model.Tick();
				}
				else
				{
					APIRequest.SleepOnApiUnauthorizedWebException(() => model.Tick());
				}
				Thread.Sleep(TICKRATE);
			}
		}
	}
}
