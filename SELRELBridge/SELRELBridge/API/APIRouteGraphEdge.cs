using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace SELRELBridge.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class APIRouteGraphEdge : IEquatable<APIRouteGraphEdge>
	{
		public int edge_id;
		public int from_vertex_id;
		public int to_vertex_id;
		public float edge_width;
		public APIGeometryType[] link_crosses_msp_layers;

		public APIRouteGraphEdge(int a_edgeId, int a_fromVertex, int a_toVertex, float a_edgeWidth, APIGeometryType[] a_linkCrossesMspTypes)
		{
			edge_id = a_edgeId;
			from_vertex_id = a_fromVertex;
			to_vertex_id = a_toVertex;
			edge_width = a_edgeWidth;
			link_crosses_msp_layers = a_linkCrossesMspTypes;
		}

		public override int GetHashCode()
		{
			return edge_id;
		}

		public bool Equals(APIRouteGraphEdge other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return edge_id == other.edge_id;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((APIRouteGraphEdge)obj);
		}

		public static bool operator ==(APIRouteGraphEdge left, APIRouteGraphEdge right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(APIRouteGraphEdge left, APIRouteGraphEdge right)
		{
			return !Equals(left, right);
		}
	}
}
