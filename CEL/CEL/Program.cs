using System;
using System.Threading;
using System.IO;
using MSWSupport;

class Program
{
    private const int TICKRATE = 500; //ms

    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        ConsoleTextWriter.Instance.SetMessageFormat("{prefix}{message}");
        ConsoleTextWriter.Instance.SetMessageParameter("prefix", "CEL: ");
		Console.SetOut(ConsoleTextWriter.Instance);
        
		// wait here, until the file cel_wait.txt has been deleted by the user
		//   this allows the programmer to attach a debugger to the process
		if (File.Exists("cel_wait.txt"))
		{
			Console.WriteLine("Please delete the file cel_wait.txt to continue...");
		}
		while (File.Exists("cel_wait.txt"))
		{
			Thread.Sleep(1000);
		}        
        
        ConsoleLogger.Info("Starting CEL");
        EnergyDistribution distribution = new EnergyDistribution();
        distribution.WaitForApiAccess();
        while (true)
        {
            APIRequest.SleepOnApiUnauthorizedWebException(() => distribution.Tick());
            Thread.Sleep(TICKRATE);
        }
    }

    static void CurrentDomain_UnhandledException(object aSender, UnhandledExceptionEventArgs aException)
    {
        ConsoleLogger.Error(((Exception) aException.ExceptionObject).Message);
    }
}

