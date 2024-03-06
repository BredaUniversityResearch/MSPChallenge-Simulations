using System;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;

class Program
{
    private const int TICKRATE = 500; //ms

    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        ConsoleLogger.Info("Starting CEL");
        EnergyDistribution distribution = new EnergyDistribution();
        while (true)
        {
            distribution.Tick();
            Thread.Sleep(TICKRATE);
        }
    }
    
    static void CurrentDomain_UnhandledException(object aSender, UnhandledExceptionEventArgs aException)
    {
        ConsoleLogger.Error(((Exception) aException.ExceptionObject).Message);
    }
}

