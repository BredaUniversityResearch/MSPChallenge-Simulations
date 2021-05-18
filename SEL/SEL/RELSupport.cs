using System;
using System.Collections.Generic;
using SEL.API;
using SELRELBridge;
using SELRELBridge.API;

namespace SEL
{
	class RELSupport
	{
		internal class APIRouteGraphEqualityComparer : IEqualityComparer<APIRouteGraphEdgeIntensity>
		{
			public bool Equals(APIRouteGraphEdgeIntensity a_x, APIRouteGraphEdgeIntensity a_y)
			{
				return a_x.edge_id == a_y.edge_id && a_x.ship_type_id == a_y.ship_type_id;
			}

			public int GetHashCode(APIRouteGraphEdgeIntensity a_obj)
			{
				int hash = 17;
				hash = hash * 23 + a_obj.edge_id;
				hash = hash * 23 + a_obj.ship_type_id;
				return hash;
			}
		}

		internal class APIRouteGraphVertexEqualityComparer : IEqualityComparer<APIRouteGraphVertex>
		{
			public bool Equals(APIRouteGraphVertex a_x, APIRouteGraphVertex a_y)
			{
				return a_x.vertex_id == a_y.vertex_id;
			}

			public int GetHashCode(APIRouteGraphVertex a_obj)
			{
				return a_obj.vertex_id;
			}
		}

		internal class APIRouteGraphEdgeEqualityComparer : IEqualityComparer<APIRouteGraphEdge>
		{
			public bool Equals(APIRouteGraphEdge a_x, APIRouteGraphEdge a_y)
			{
				return a_x.m_uniqueEdgeHash == a_y.m_uniqueEdgeHash;
			}

			public int GetHashCode(APIRouteGraphEdge a_obj)
			{
				return a_obj.m_uniqueEdgeHash;
			}
		}

		private BridgeServer m_bridgeServer;

		public RELSupport(string a_watchdogToken)
		{
			m_bridgeServer = new BridgeServer(a_watchdogToken);
		}

		public void SubmitResults(RouteManager routeManager, RouteIntensityManager routeIntensityManager, int timeMonth, ShippingPortManager portManager)
		{
			HashSet<APIRouteGraphVertex> points = new HashSet<APIRouteGraphVertex>(new APIRouteGraphVertexEqualityComparer());
			HashSet<APIRouteGraphEdge> edges = new HashSet<APIRouteGraphEdge>(new APIRouteGraphEdgeEqualityComparer());
			HashSet<APIRouteGraphEdgeIntensity> edgeIntensities = new HashSet<APIRouteGraphEdgeIntensity>(new APIRouteGraphEqualityComparer());

			RouteIntensity[] intensities = routeIntensityManager.GetAllAbsoluteRouteIntensities(timeMonth, portManager);
			foreach (RouteIntensity routeIntensity in intensities)
			{
				Route pathedRoute = routeManager.FindCachedRoute(routeIntensity);
				if (pathedRoute != null)
				{
					AddRouteData(points, edges, edgeIntensities, pathedRoute, routeIntensity.Intensity);
				}
				else
				{
					ErrorReporter.ReportError(EErrorSeverity.Warning, $"Could not find route for ship type {routeIntensity.ShipTypeId} from {routeIntensity.SourcePort.PortName} to " +
																	  $"{routeIntensity.DestinationPort.PortName}. Routing graph submitted to server will be incomplete.");
				}
			}

			SELOutputData data = new SELOutputData();
			data.m_simulatedMonth = timeMonth;
			data.m_routeGraphPoints = new APIRouteGraphVertex[points.Count];
			points.CopyTo(data.m_routeGraphPoints);
			data.m_routeGraphEdges = new APIRouteGraphEdge[edges.Count];
			edges.CopyTo(data.m_routeGraphEdges);
			data.m_routeGraphIntensities = new APIRouteGraphEdgeIntensity[edgeIntensities.Count];
			edgeIntensities.CopyTo(data.m_routeGraphIntensities);

			Console.WriteLine($"SELREL\t| Sending SEL output data to REL for month {data.m_simulatedMonth}");
			m_bridgeServer.Submit(data);
		}

		private void AddRouteData(HashSet<APIRouteGraphVertex> routeGraphPoints, HashSet<APIRouteGraphEdge> routeGraphEdges, HashSet<APIRouteGraphEdgeIntensity> routeGraphIntensities, Route pathedRoute, int intensity)
		{
			int lastVertexId = pathedRoute.FromVertex.vertexId;

			LaneEdge firstEdge = pathedRoute.GetFirstLaneEdge();
			if (firstEdge.m_from.vertexId == lastVertexId)
			{
				routeGraphPoints.Add(new APIRouteGraphVertex(firstEdge.m_from.vertexId, firstEdge.m_from.position.x, firstEdge.m_from.position.y));

			}
			else if (firstEdge.m_to.vertexId == lastVertexId)
			{
				routeGraphPoints.Add(new APIRouteGraphVertex(firstEdge.m_to.vertexId, firstEdge.m_to.position.x, firstEdge.m_to.position.y));
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
				routeGraphPoints.Add(new APIRouteGraphVertex(vertexToPush.vertexId, vertexToPush.position.x, vertexToPush.position.y));

				int edgeId = TryAddLaneEdge(lastVertexId, vertexToPush.vertexId, edge.m_laneWidth, edge.GetRestrictionLayerComposition(), routeGraphEdges);

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

		private int TryAddLaneEdge(int fromVertexId, int toVertexId, float edgeWidth, GeometryType[] edgeCrossesGeometryTypes, HashSet<APIRouteGraphEdge> edges)
		{
			APIGeometryType[] crossesGeometryTypes = null;
			if (edgeCrossesGeometryTypes != null)
			{
				crossesGeometryTypes = new APIGeometryType[edgeCrossesGeometryTypes.Length];
				for (int i = 0; i < crossesGeometryTypes.Length; ++i)
				{
					crossesGeometryTypes[i] = new APIGeometryType(edgeCrossesGeometryTypes[i].LayerId,
						edgeCrossesGeometryTypes[i].LayerType);
				}
			}


			APIRouteGraphEdge edge = new APIRouteGraphEdge(edges.Count, fromVertexId, toVertexId, edgeWidth, crossesGeometryTypes);
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
	}
}