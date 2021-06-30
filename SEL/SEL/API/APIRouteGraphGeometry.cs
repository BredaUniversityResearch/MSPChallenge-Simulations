using System;
using System.Diagnostics.CodeAnalysis;

namespace SEL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[SuppressMessage("ReSharper", "NotAccessedField.Global")]
	class APIRouteGraphGeometry
	{
		public double[][] geometry;
		public string fid;
		public int intensity;

		public APIRouteGraphGeometry(Route route, int intensity)
		{
			int numVertices = route.GetRouteEdgeCount() + 1;
			geometry = new double[numVertices][];
			int vertexCount = 0;
			int lastVertexId = -1;
			foreach (LaneEdge edge in route.GetRouteEdges())
			{
				if (vertexCount == 0)
				{
					lastVertexId = route.FromVertex.vertexId;
					if (edge.m_from.vertexId == lastVertexId)
					{
						geometry[vertexCount + 0] = new[] { edge.m_from.position.x, edge.m_from.position.y };
						geometry[vertexCount + 1] = new[] { edge.m_to.position.x, edge.m_to.position.y };
						lastVertexId = edge.m_to.vertexId;
						vertexCount += 2;
					}
					else if (edge.m_to.vertexId == lastVertexId)
					{
						geometry[vertexCount + 0] = new[] { edge.m_to.position.x, edge.m_to.position.y };
						geometry[vertexCount + 1] = new[] { edge.m_from.position.x, edge.m_from.position.y };
						lastVertexId = edge.m_from.vertexId;
						vertexCount += 2;
					}
					else
					{
						throw new Exception("Starting vertex is not in the first edge?");
					}
				}
				else
				{
					if (edge.m_from.vertexId != lastVertexId && edge.m_to.vertexId != lastVertexId)
					{
						throw new Exception("Unconnected vertex found in route?");
					}

					GeometryVertex vertexToPush = (edge.m_from.vertexId == lastVertexId) ? edge.m_to : edge.m_from;
					geometry[vertexCount] = new[] {vertexToPush.position.x, vertexToPush.position.y};
					++vertexCount;
					lastVertexId = vertexToPush.vertexId;
				}
			}

			this.intensity = intensity;
			fid = $"SEL_ROUTE_{RouteManager.CreateRouteHash(route)}";
		}
	}
}
