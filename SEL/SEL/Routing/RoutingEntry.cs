namespace SEL.Routing
{
	/// <summary>
	/// These routing entries define what percentage of the ships need to be routed to what port
	/// </summary>
	class RoutingEntry
	{
		public byte shipTypeId { get; private set; }
		public ShippingPort sourcePort { get; private set; }
		public ShippingPort destinationPort { get; private set; }
		public float sourcePortPercentage { get; set; } //The percentage of ships from the source port that need to be routed here.

		public RoutingEntry(byte shipTypeId, ShippingPort sourcePort, ShippingPort destinationPort, float sourcePortPercentage)
		{
			this.shipTypeId = shipTypeId;
			this.sourcePort = sourcePort;
			this.destinationPort = destinationPort;
			this.sourcePortPercentage = sourcePortPercentage;
		}
	}
}
