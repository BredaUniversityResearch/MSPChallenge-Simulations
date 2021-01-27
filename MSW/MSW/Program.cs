using System;
using System.Diagnostics;
using System.Threading;

namespace MSW
{
	class Program
	{
		//TODO: add firewall rules on the required ports. 
		/*INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
    Type.GetTypeFromProgID("HNetCfg.FWRule"));
firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
firewallRule.Description = "Enables eATM REST Web Service adapter       
    traffic.";
firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
firewallRule.Enabled = true;
firewallRule.InterfaceTypes = "All";
firewallRule.Name = "MyPort";
firewallRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
firewallRule.LocalPorts = "9600";  
INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
    Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
firewallPolicy.Rules.Add(firewallRule);*/

		private const long TickTimeMs = 50;

		static void Main(string[] a_args)
		{
			Console.WriteLine("Starting MSP2050 Simulation Watchdog...");

			Watchdog watchdog = new Watchdog();
			Stopwatch stopwatch = new Stopwatch();
			Console.WriteLine("Watchdog started successfully, waiting for requests...");

			while (true)
			{
				stopwatch.Restart();
				watchdog.Tick();

				long timeToSleep = TickTimeMs - stopwatch.ElapsedMilliseconds;
				if (timeToSleep > 0)
				{
					Thread.Sleep((int)timeToSleep);
				}
			}
		}
	}
}
