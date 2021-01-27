using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace SELRELBridge.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class APIRouteGraphEdge
	{
		public int edge_id;
		public int from_vertex_id;
		public int to_vertex_id;
		public float edge_width;
		[JsonIgnore]
		public readonly int m_uniqueEdgeHash;

		public APIRouteGraphEdge(int a_edgeId, int a_fromVertex, int a_toVertex, float a_edgeWidth)
		{
			edge_id = a_edgeId;
			from_vertex_id = a_fromVertex;
			to_vertex_id = a_toVertex;
			edge_width = a_edgeWidth;
			m_uniqueEdgeHash = CreateHash(a_fromVertex, a_toVertex, a_edgeWidth);
		}

		public override int GetHashCode()
		{
			return m_uniqueEdgeHash;
		}

		private static int CreateHash(int a_fromVertex, int a_toVertex, float a_edgeWidth)
		{
			int hash = 17;
			hash = hash * 24 + a_fromVertex;
			hash = hash * 24 + a_toVertex;
			hash = hash * 24 + a_edgeWidth.GetHashCode();
			return hash;
		}
	}
}
