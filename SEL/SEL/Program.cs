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
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
	        ConsoleTextWriter.Instance.SetMessageFormat("{prefix}{message}");
	        ConsoleTextWriter.Instance.SetMessageParameter("prefix", "SEL: ");
			Console.SetOut(ConsoleTextWriter.Instance);
			ConsoleLogger.Info($"Starting MSP2050 Shipping EmuLation version {typeof(Program).Assembly.GetName().Version}");

			// wait here, until the file sel_wait.txt has been deleted by the user
			//   this allows the programmer to attach a debugger to the process
			if (File.Exists("sel_wait.txt"))
			{
				Console.WriteLine("Please delete the file sel_wait.txt to continue...");
			}
			while (File.Exists("sel_wait.txt"))
			{
				Thread.Sleep(1000);
				Console.WriteLine("Please delete the file sel_wait.txt to continue...");
			}			
			
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

		static void CurrentDomain_UnhandledException(object aSender, UnhandledExceptionEventArgs aException)
		{
			ConsoleLogger.Error(((Exception) aException.ExceptionObject).Message);
		}
	}
}
