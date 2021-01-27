using System.Diagnostics.CodeAnalysis;

namespace REL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MarinAPIResponseShipCollisionData
	{
		public int x;
		public int y;
		public byte ship_type;
		public double collisions;
	};
}