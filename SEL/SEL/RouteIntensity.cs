namespace SEL
{
	/// <summary>
	/// Simple class describing the intensity (number of boats) we have for the current time unit
	/// traveling from source to destination for a specific ship type id.
	/// </summary>
	class RouteIntensity
	{
		public ShippingPort SourcePort { get; private set; }
		public ShippingPort DestinationPort { get; private set; }
		public byte ShipTypeId { get; private set; }
		public int Intensity { get; private set; } //Intensity expressed in the number of ships that take this route.

		public RouteIntensity(ShippingPort sourcePort, ShippingPort destinationPort, byte shipTypeId, int intensity)
		{
			SourcePort = sourcePort;
			DestinationPort = destinationPort;
			ShipTypeId = shipTypeId;
			Intensity = intensity;
		}
	}
}
