using System;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;

class Program
{
    private const int TICKRATE = 500; //ms

    static void Main(string[] args)
    {
		Console.WriteLine("Starting CEL");
        EnergyDistribution distribution = new EnergyDistribution();
        while (true)
        {
            distribution.Tick();
            Thread.Sleep(TICKRATE);
        }
    }
}

