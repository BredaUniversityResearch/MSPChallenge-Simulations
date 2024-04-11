using System;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
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

