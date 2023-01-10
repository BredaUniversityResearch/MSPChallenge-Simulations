using System.Diagnostics.CodeAnalysis;

namespace SELRELBridge.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class APIRouteGraphEdgeIntensity
	{
		public int edge_id;
		public byte ship_type_id;
		public int intensity;

		public APIRouteGraphEdgeIntensity(int a_edgeId, byte a_shipTypeId, int a_intensity)
		{
			edge_id = a_edgeId;
			ship_type_id = a_shipTypeId;
			intensity = a_intensity;
		}
	}
}
