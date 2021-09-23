
using System;
using System.Threading;

namespace SEL
{
	class Program
	{
		private const int TICKRATE = 500; //ms

		static void Main(string[] args)
		{
			Console.WriteLine("Starting MSP2050 Shipping EmuLation version {0}", typeof(Program).Assembly.GetName().Version);

			ShippingModel model = new ShippingModel();
			while (true)
			{
				model.Tick();
				Thread.Sleep(TICKRATE);
			}
		}
	}
}
