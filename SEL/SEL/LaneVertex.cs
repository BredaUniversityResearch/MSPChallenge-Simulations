using System;
using System.Collections.Generic;

namespace SEL
{
	/* Vertex on the lane graph */
	class LaneVertex: GeometryVertex
	{
		private List<LaneEdge> m_connections = new List<LaneEdge>();
		public int geometryId { get; private set; }

		public LaneVertex(double x, double y, int a_geometryId)
			: base(x, y)
		{
			geometryId = a_geometryId;
		}

		public void AddConnection(LaneEdge edge)
		{
			if (edge.m_from != this && edge.m_to != this)
			{
				throw new ArgumentException("Edge is not set to connect to this vertex");
			}

			m_connections.Add(edge);
		}

		public IEnumerable<LaneEdge> GetConnections()
		{
			return m_connections;
		}

		public override string ToString()
		{
			return $"LaneVertex: {vertexId} : {position.x}, {position.y}";
		}
	}
}
