using System.Diagnostics.CodeAnalysis;

namespace REL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MSPAPIRouteGraphEdge
	{
		public int edge_id;
		public int from_vertex_id;
		public int to_vertex_id;
		public float edge_width;
	}
}
