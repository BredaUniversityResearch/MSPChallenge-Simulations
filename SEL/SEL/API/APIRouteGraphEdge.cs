using Newtonsoft.Json;

namespace SEL.API
{
	class APIRouteGraphEdge
	{
		public int edge_id;
		public int from_vertex_id;
		public int to_vertex_id;
		public float edge_width;
		[JsonIgnore]
		public readonly int uniqueEdgeHash;

		public APIRouteGraphEdge(int edgeId, int fromVertex, int toVertex, float edgeWidth)
		{
			edge_id = edgeId;
			from_vertex_id = fromVertex;
			to_vertex_id = toVertex;
			edge_width = edgeWidth;
			uniqueEdgeHash = CreateHash(fromVertex, toVertex, edgeWidth);
		}

		public override int GetHashCode()
		{
			return uniqueEdgeHash;
		}

		private static int CreateHash(int fromVertex, int toVertex, float edgeWidth)
		{
			int hash = 17;
			hash = hash * 24 + fromVertex;
			hash = hash * 24 + toVertex;
			hash = hash * 24 + edgeWidth.GetHashCode();
			return hash;
		}
	}
}
