using System.Diagnostics.CodeAnalysis;

namespace REL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MarinAPIProcessResponse
	{
		public MarinAPIResponseShipCollisionData[] ship_collision_data;
		public MarinAPIResponseGeometryData[] restriction_contacts;
	}
}
