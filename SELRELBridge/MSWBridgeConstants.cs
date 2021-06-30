using System.Net;

namespace SELRELBridge
{
	internal static class MSWBridgeConstants
	{
		public static readonly IPAddress DiscoveryMulticastAddress = IPAddress.Parse("224.0.0.42");
		public const ushort DiscoveryMulticastPort = 45002;
	}
}
