using System;
using System.Collections.Generic;
using SEL.API;

namespace SEL
{
	class RouteGraphSubmitter
	{
		public void SubmitRouteGraph(IApiConnector apiConnector, RouteManager routeManager, RouteIntensityManager routeIntensityManager, int timeMonth,
			ShippingPortManager portManager)
		{
			HashSet<APIRouteGraphVertex> routeGraphPoints = new HashSet<APIRouteGraphVertex>(new APIRouteGraphVertexEqualityComparer());
			HashSet<APIRouteGraphEdge> routeGraphEdges = new HashSet<APIRouteGraphEdge>(new APIRouteGraphEdgeEqualityComparer());
			HashSet<APIRouteGraphEdgeIntensity> routeGraphIntensities = new HashSet<APIRouteGraphEdgeIntensity>(new APIRouteGraphEqualityComparer());

			RouteIntensity[] intensities = routeIntensityManager.GetAllAbsoluteRouteIntensities(timeMonth, portManager);
			foreach (RouteIntensity routeIntensity in intensities)
			{
				Route pathedRoute = routeManager.FindCachedRoute(routeIntensity);
				if (pathedRoute != null)
				{
					AddRouteData(routeGraphPoints, routeGraphEdges, routeGraphIntensities, pathedRoute, routeIntensity.Intensity);
				}
				else
				{
					ErrorReporter.ReportError(EErrorSeverity.Warning, $"Could not find route for ship type {routeIntensity.ShipTypeId} from {routeIntensity.SourcePort.PortName} to " +
																	  $"{routeIntensity.DestinationPort.PortName}. Routing graph submitted to server will be incomplete.");
				}
			}

			apiConnector.SubmitRouteIntensityData(routeGraphPoints, routeGraphEdges, routeGraphIntensities);
		}

		private void AddRouteData(HashSet<APIRouteGraphVertex> routeGraphPoints, HashSet<APIRouteGraphEdge> routeGraphEdges, HashSet<APIRouteGraphEdgeIntensity> routeGraphIntensities, Route pathedRoute, int intensity)
		{
			int lastVertexId = pathedRoute.FromVertex.vertexId;

			LaneEdge firstEdge = pathedRoute.GetFirstLaneEdge();
			if (firstEdge.m_from.vertexId == lastVertexId)
			{
				routeGraphPoints.Add(new APIRouteGraphVertex(firstEdge.m_from));

			}
			else if (firstEdge.m_to.vertexId == lastVertexId)
			{
				routeGraphPoints.Add(new APIRouteGraphVertex(firstEdge.m_to));
			}
			else
			{
				throw new Exception("Starting vertex is not in the first edge?");
			}

			foreach (LaneEdge edge in pathedRoute.GetRouteEdges())
			{
				if (edge.m_from.vertexId != lastVertexId && edge.m_to.vertexId != lastVertexId)
				{
					throw new Exception("Unconnected vertex found in route?");
				}

				GeometryVertex vertexToPush = (edge.m_from.vertexId == lastVertexId) ? edge.m_to : edge.m_from;
				routeGraphPoints.Add(new APIRouteGraphVertex(vertexToPush));

				int edgeId = TryAddLaneEdge(lastVertexId, vertexToPush.vertexId, edge.m_laneWidth, routeGraphEdges);

				APIRouteGraphEdgeIntensity apiIntensity = new APIRouteGraphEdgeIntensity(edgeId, pathedRoute.ShipTypeInfo.ShipTypeId, intensity);
				if (routeGraphIntensities.TryGetValue(apiIntensity, out var existingIntensity))
				{
					existingIntensity.intensity += intensity;
				}
				else
				{
					routeGraphIntensities.Add(apiIntensity);
				}

				lastVertexId = vertexToPush.vertexId;
			}
		}

		private int TryAddLaneEdge(int fromVertexId, int toVertexId, float edgeWidth, HashSet<APIRouteGraphEdge> edges)
		{
			APIRouteGraphEdge edge = new APIRouteGraphEdge(edges.Count, fromVertexId, toVertexId, edgeWidth);
			if (edges.TryGetValue(edge, out APIRouteGraphEdge existingEdge))
			{
				return existingEdge.edge_id;
			}
			else
			{
				edges.Add(edge);
				return edge.edge_id;
			}
		}

		//RouteIntensity[] intensities = routeIntensityManager.GetAllAbsoluteRouteIntensities(timeMonth, portManager);
		//	List<APIRouteGraphGeometry> geometriesToSubmit = new List<APIRouteGraphGeometry>(intensities.Length);
		//	foreach (RouteIntensity routeIntensity in intensities)
		//	{
		//		Route pathedRoute = routeManager.FindCachedRoute(routeIntensity);
		//		if (pathedRoute != null)
		//		{
		//			geometriesToSubmit.Add(new APIRouteGraphGeometry(pathedRoute, routeIntensity.Intensity));
		//		}
		//		else
		//		{
		//			ErrorReporter.ReportError(EErrorSeverity.Warning, $"Could not find route for ship type {routeIntensity.ShipTypeId} from {routeIntensity.SourcePort.PortName} to " +
		//				$"{routeIntensity.DestinationPort.PortName}. Routing graph submitted to server will be incomplete.");
		//		}
		//	}

		//	const int MaxRoutesPerSubmitBatch = 128;
		//	int numRoutesToSubmit = geometriesToSubmit.Count;
		//	for (int i = 0; i < numRoutesToSubmit; i += MaxRoutesPerSubmitBatch)
		//	{
		//		Console.Write($"Submitting route geometry step {i / MaxRoutesPerSubmitBatch} / {numRoutesToSubmit / MaxRoutesPerSubmitBatch}\r");
		//		apiConnector.SubmitRouteIntensityGeometry(geometriesToSubmit.GetRange(i, Math.Min(numRoutesToSubmit - i, MaxRoutesPerSubmitBatch)));
		//	}
		//}
	}

	class APIRouteGraphEqualityComparer : IEqualityComparer<APIRouteGraphEdgeIntensity>
	{
		public bool Equals(APIRouteGraphEdgeIntensity x, APIRouteGraphEdgeIntensity y)
		{
			return x.edge_id == y.edge_id && x.ship_type_id == y.ship_type_id;
		}

		public int GetHashCode(APIRouteGraphEdgeIntensity obj)
		{
			int hash = 17;
			hash = hash * 23 + obj.edge_id;
			hash = hash * 23 + obj.ship_type_id;
			return hash;
		}
	}

	class APIRouteGraphVertexEqualityComparer : IEqualityComparer<APIRouteGraphVertex>
	{
		public bool Equals(APIRouteGraphVertex x, APIRouteGraphVertex y)
		{
			return x.vertex_id == y.vertex_id;
		}

		public int GetHashCode(APIRouteGraphVertex obj)
		{
			return obj.vertex_id;
		}
	}

	class APIRouteGraphEdgeEqualityComparer : IEqualityComparer<APIRouteGraphEdge>
	{
		public bool Equals(APIRouteGraphEdge x, APIRouteGraphEdge y)
		{
			return x.uniqueEdgeHash == y.uniqueEdgeHash;
		}

		public int GetHashCode(APIRouteGraphEdge obj)
		{
			return obj.uniqueEdgeHash;
		}
	}
}