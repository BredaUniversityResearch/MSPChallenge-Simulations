using System;
using System.IO;
using MSWSupport;
namespace MEL
{
    class Start
    {
        public static void Main(string[] args)
        {
	        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
	        ConsoleTextWriter.Instance.SetMessageFormat("{prefix}{message}");
	        ConsoleTextWriter.Instance.SetMessageParameter("prefix", "MEL: ");
			Console.SetOut(ConsoleTextWriter.Instance);

			// wait here, until the file mel_wait.txt has been deleted by the user
			//   this allows the programmer to attach a debugger to the process
			if (File.Exists("mel_wait.txt"))
			{
				Console.WriteLine("Please delete the file mel_wait.txt to continue...");
			}
			while (File.Exists("mel_wait.txt"))
			{
				System.Threading.Thread.Sleep(1000);
			}

            MEL mel = new MEL();
            mel.WaitForApiAccess();
            while(true) {
				System.Threading.Thread.Sleep(MEL.TICK_DELAY_MS);
				try {
					APIRequest.SleepOnApiUnauthorizedWebException(() => mel.Tick());
		        }
		        catch (SessionApiGoneWebException ex)
		        {
			        Console.WriteLine("Session API gone, exiting...");
			        Environment.Exit(0);
		        }				
			}
		}

        static void CurrentDomain_UnhandledException(object aSender, UnhandledExceptionEventArgs aException)
        {
	        ConsoleLogger.Error(((Exception) aException.ExceptionObject).Message);
        }
	}
}
