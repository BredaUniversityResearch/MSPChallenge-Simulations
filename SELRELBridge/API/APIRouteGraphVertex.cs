using System;
using System.Diagnostics.CodeAnalysis;

namespace SELRELBridge.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public sealed class APIRouteGraphVertex : IEquatable<APIRouteGraphVertex>
	{
		public int vertex_id;
		public double position_x;
		public double position_y;

		public APIRouteGraphVertex(int a_vertexId, double a_positionX, double a_positionY)
		{
			vertex_id = a_vertexId;
			position_x = a_positionX;
			position_y = a_positionY;
		}

		public bool Equals(APIRouteGraphVertex other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return vertex_id == other.vertex_id;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((APIRouteGraphVertex) obj);
		}

		public override int GetHashCode()
		{
			return vertex_id;
		}
	}
}
