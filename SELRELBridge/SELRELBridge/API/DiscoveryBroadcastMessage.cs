using System.Diagnostics.CodeAnalysis;

namespace SELRELBridge.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	internal class DiscoveryBroadcastMessage
	{
		public string watchdog_token;
		public int sel_connection_port;
	}
}
