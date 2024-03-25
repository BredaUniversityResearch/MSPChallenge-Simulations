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
        ConsoleTextWriter.Instance.SetMessageFormat("{prefix}{message}");
        ConsoleTextWriter.Instance.SetMessageParameter("prefix", "CEL: ");
		Console.SetOut(ConsoleTextWriter.Instance);
        Console.WriteLine("Starting CEL");
        EnergyDistribution distribution = new EnergyDistribution();
        distribution.WaitForApiAccess();
        while (true)
        {
            APIRequest.SleepOnApiUnauthorizedWebException(() => distribution.Tick());
            Thread.Sleep(TICKRATE);
        }
    }
}

