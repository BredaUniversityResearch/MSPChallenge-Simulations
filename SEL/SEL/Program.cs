using System;
using System.Threading;
namespace SEL
{
	class Program
	{
		
		private const int TICKRATE = 500; //ms

		static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			ConsoleLogger.Info($"Starting MSP2050 Shipping EmuLation version {typeof(Program).Assembly.GetName().Version}");

			ShippingModel model = new ShippingModel();
			while (true)
			{
				model.Tick();
				Thread.Sleep(TICKRATE);
			}
		}
		
		static void CurrentDomain_UnhandledException(object aSender, UnhandledExceptionEventArgs aException)
		{
			ConsoleLogger.Error(((Exception) aException.ExceptionObject).Message);
		}
	}
}
