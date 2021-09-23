#if DEBUG
#define ROUTE_DEBUG_VERIFY
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;

using HeuristicValue = System.Int64;

namespace SEL
{
	static class RouteFinder
	{
		private const double HEURISTIC_COST_MULTIPLIER = 1.0;

		private class ReverseFCostSort : IComparer<RouteNode>
		{
			public static readonly ReverseFCostSort Instance = new ReverseFCostSort();

			public int Compare(RouteNode x, RouteNode y)
			{
				if (x.fCost < y.fCost)
				{
					return 1;
				}
				else if (x.fCost > y.fCost)
				{
					return -1;
				}
				else
				{
					return 0;
				}
			}
		}

		private class ReverseHeuristicValueSort : IComparer<KeyValuePair<HeuristicValue, RouteNode>>
		{
			public static readonly ReverseHeuristicValueSort Instance = new ReverseHeuristicValueSort();

			public int Compare(KeyValuePair<HeuristicValue, RouteNode> lhs, KeyValuePair<HeuristicValue, RouteNode> rhs)
			{
				if (lhs.Key < rhs.Key)
				{
					return 1;
				}
				else if (lhs.Key > rhs.Key)
				{
					return -1;
				}
				else
				{
					return lhs.Value.m_currentVertex.vertexId.CompareTo(rhs.Value.m_currentVertex.vertexId);
				}
			}
		}

		/// <summary>
		/// A* node structure.
		/// </summary>
		private class RouteNode
		{
			public LaneVertex m_currentVertex = null;
			public LaneEdge m_connectingEdge = null;
			public RouteNode m_previousNode = null;

			private HeuristicValue m_gCost;
			private HeuristicValue m_hCost;
			private HeuristicValue m_fCost;

			public HeuristicValue gCost { get { return m_gCost; } set { m_gCost = value; UpdateFCost(); } } //Cost from start to this node.
			public HeuristicValue hCost { get { return m_hCost; } set { m_hCost = value; UpdateFCost(); } } //Heuristic cost to destination.
			public HeuristicValue fCost { get { return m_fCost; } } //Full estimated cost (g + h).

			private void UpdateFCost()
			{
				if ((HeuristicValue.MaxValue - m_gCost) - m_hCost < 0)
				{
					Console.WriteLine("FCost overflow! Reduce the cost multiplier in the config files please.");
					m_fCost = HeuristicValue.MaxValue;
				}
				else
				{
					m_fCost = m_gCost + m_hCost;
				}
			}
		}

		private class OpenNodeList
		{
			private List<KeyValuePair<HeuristicValue, RouteNode>> m_openListFCostReverseSorted = new List<KeyValuePair<HeuristicValue, RouteNode>>(1024);
			private Dictionary<int, RouteNode> m_routeNodesByVertexId = new Dictionary<int, RouteNode>();

			public int Count { get { return m_openListFCostReverseSorted.Count; } }

			public void Add(RouteNode node)
			{
				KeyValuePair<HeuristicValue, RouteNode> newNode = new KeyValuePair<HeuristicValue, RouteNode>(node.fCost, node);
				int indexToInsert = m_openListFCostReverseSorted.BinarySearch(newNode, ReverseHeuristicValueSort.Instance);
				if (indexToInsert < 0)
				{
					indexToInsert = ~indexToInsert;
				}

				m_openListFCostReverseSorted.Insert(indexToInsert, newNode);
				m_routeNodesByVertexId.Add(node.m_currentVertex.vertexId, node);

				VerifyReverseSorted(m_openListFCostReverseSorted);
			}

			public RouteNode PopFront()
			{
				int index = m_openListFCostReverseSorted.Count - 1;
				RouteNode result = m_openListFCostReverseSorted[index].Value;
				m_openListFCostReverseSorted.RemoveAt(index);
				m_routeNodesByVertexId.Remove(result.m_currentVertex.vertexId);
				return result;
			}

			public void Reinsert(HeuristicValue oldFCost, RouteNode routeNode)
			{
				int index = m_openListFCostReverseSorted.BinarySearch(new KeyValuePair<long, RouteNode>(oldFCost, routeNode), ReverseHeuristicValueSort.Instance);
				if (index >= 0)
				{
					if (m_openListFCostReverseSorted[index].Value != routeNode)
					{
						throw new Exception("RouteNode found at the index not expected.");
					}

					m_openListFCostReverseSorted.RemoveAt(index);
					bool result = m_routeNodesByVertexId.Remove(routeNode.m_currentVertex.vertexId);
					if (!result)
					{
						throw new Exception("routeNode not found in the list to remove");
					}
				}
				else
				{
					throw new Exception("Could not find node with oldFCost!");
				}

				Add(routeNode);
			}

			public RouteNode FindRouteNodeByVertexId(int vertexId)
			{
				m_routeNodesByVertexId.TryGetValue(vertexId, out var result);
				return result;
			}

			[Conditional("ROUTE_DEBUG_VERIFY")]
			public void VerifyReverseSorted(IEnumerable<KeyValuePair<HeuristicValue, RouteNode>> list)
			{
				//List should be sorted from highest value to lowest value. 
				HeuristicValue lastFCost = HeuristicValue.MaxValue;
				foreach (KeyValuePair<HeuristicValue, RouteNode> node in list)
				{
					if (node.Value.fCost > lastFCost)
						throw new Exception("List is not sorted in the correct order anymore!");
					lastFCost = node.Value.fCost;
				}
			}
		};

		/// <summary>
		/// Per-query information.
		/// </summary>
		private class RouteQuery
		{
			public OpenNodeList m_openList = new OpenNodeList();
			public Dictionary<int, RouteNode> m_closedList = new Dictionary<int, RouteNode>();
			public LaneVertex m_sourceVertex = null;
			public LaneVertex m_destinationVertex = null;
			public RouteNode m_endNode = null;
			public bool m_queryComplete = false;

			public double m_implicitEdgeCostMultiplier = 1.0;
			public int m_shipTypeId = 0;
		}

		/// <summary>
		/// Tries to find a route from source to destination if there's any possible.
		/// </summary>
		/// <param name="source">starting point of the search</param>
		/// <param name="destination">end point of the search</param>
		/// <returns>Route if possible, null if no route can be found.</returns>
		public static Route FindRoute(LaneVertex source, LaneVertex destination, ShipType shipParameters)
		{
			if (source == destination)
				throw new ArgumentException($"Route finding with Source == destination... Geometry IDs: Source: {source.geometryId}, Destination: {destination.geometryId}. Ship type: {shipParameters.ShipTypeName} ({shipParameters.ShipTypeId})");
			Route result = null;

			RouteQuery query = new RouteQuery();
			query.m_sourceVertex = source;
			query.m_destinationVertex = destination;
			query.m_implicitEdgeCostMultiplier = 1.0f / shipParameters.ShipAgilityValue;
			query.m_shipTypeId = shipParameters.ShipTypeId;

			RouteNode sourceNode = new RouteNode();
			sourceNode.m_currentVertex = source;
			sourceNode.m_previousNode = null;
			sourceNode.gCost = 0;
			sourceNode.hCost = GetHeuristicCost(source, destination, query);

			query.m_openList.Add(sourceNode);

			RunQueryComplete(query);

			if (query.m_endNode != null)
			{
				result = BuildRoute(query, shipParameters);
			}
			else if (SELConfig.Instance.ShouldOutputFailedRoutes())
			{
				CreateRouteQueryDebugMap(source, destination, query, true);
			}

			if (SELConfig.Instance.IsRouteFinderDebugEnabled())
			{
				CreateRouteQueryDebugMap(source, destination, query, false);
			}

			return result;
		}

		private static void RunQueryComplete(RouteQuery query)
		{
			while (!query.m_queryComplete)
			{
				RunQueryStep(query);
			}
		}

		private static void RunQueryStep(RouteQuery query)
		{
			//Get the last node, e.g. the node with the lowest fCost and move it to the closed list.
			RouteNode currentNode = query.m_openList.PopFront();
			query.m_closedList.Add(currentNode.m_currentVertex.vertexId, currentNode);

			if (currentNode.m_currentVertex == query.m_destinationVertex)
			{
				//Route completed. 
				query.m_queryComplete = true;
				query.m_endNode = currentNode;
				return;
			}

			DiscoverNewNodes(currentNode, query);

			if (query.m_openList.Count == 0)
			{
				//Exhausted all nodes. Can't find a connection to destination.
				query.m_queryComplete = true;
			}
		}

		private static HeuristicValue GetHeuristicCost(LaneVertex fromVertex, LaneVertex toVertex, RouteQuery query)
		{
			Vector2D deltaPosition = toVertex.position - fromVertex.position;
			return (HeuristicValue)(deltaPosition.Magnitude() * HEURISTIC_COST_MULTIPLIER);
		}

		private static void DiscoverNewNodes(RouteNode fromNode, RouteQuery query)
		{
			foreach (LaneEdge edge in fromNode.m_currentVertex.GetConnections())
			{
				if (!edge.IsShipTypeAllowed(query.m_shipTypeId))
				{
					continue;
				}

				if (!edge.IsTraversableFrom(fromNode.m_currentVertex))
				{
					continue;
				}

				LaneVertex otherVertex = (edge.typedFrom == fromNode.m_currentVertex) ? edge.typedTo : edge.typedFrom;
				if (query.m_closedList.ContainsKey(otherVertex.vertexId))
				{
					continue;
				}

				RouteNode existingNode = query.m_openList.FindRouteNodeByVertexId(otherVertex.vertexId);
				if (existingNode != null)
				{
					//We've already found a node in the open list for this vertex see if we can improve it's path.
					HeuristicValue tentativeGCost = fromNode.gCost + GetEdgeCost(edge, query);
					if (existingNode.gCost > tentativeGCost)
					{
						long oldFCost = existingNode.fCost;
						existingNode.gCost = tentativeGCost;
						existingNode.m_connectingEdge = edge;
						existingNode.m_previousNode = fromNode;
						query.m_openList.Reinsert(oldFCost, existingNode);
					}
				}
				else
				{
					RouteNode node = new RouteNode();
					node.m_currentVertex = otherVertex;
					node.m_connectingEdge = edge;
					node.m_previousNode = fromNode;
					node.gCost = fromNode.gCost + GetEdgeCost(edge, query);
					node.hCost = GetHeuristicCost(otherVertex, query.m_destinationVertex, query);
					query.m_openList.Add(node);
				}
			}
		}

		private static HeuristicValue GetEdgeCost(LaneEdge edge, RouteQuery query)
		{
			if (edge.m_laneType == ELaneEdgeType.Merge)
			{
				return default(HeuristicValue);
			}

			double costMultiplier = HEURISTIC_COST_MULTIPLIER * edge.GetTravelCostMultiplier(query.m_shipTypeId);
			if (edge.m_laneType == ELaneEdgeType.Implicit)
			{
				costMultiplier *= query.m_implicitEdgeCostMultiplier;
			}

			return (HeuristicValue)(edge.m_distance * costMultiplier);
		}

		private static Route BuildRoute(RouteQuery query, ShipType shipParameters)
		{
			Route result = new Route(query.m_sourceVertex, query.m_destinationVertex, shipParameters);
			RouteNode currentNode = query.m_endNode;
			while (currentNode != null)
			{
				if (currentNode.m_connectingEdge != null)
				{
					result.InsertFirstEdge(currentNode.m_connectingEdge);
				}
				currentNode = currentNode.m_previousNode;
			}
			return result;
		}

		private static void CreateRouteQueryDebugMap(LaneVertex source, LaneVertex destination, RouteQuery query, bool drawClosedRoutes)
		{
			List<LaneEdge> closedEdges = new List<LaneEdge>();
			List<LaneVertex> closedVertices = new List<LaneVertex>();
			foreach (KeyValuePair<int, RouteNode> closedNode in query.m_closedList)
			{
				closedVertices.Add(closedNode.Value.m_currentVertex);
				if (drawClosedRoutes)
				{
					if (closedNode.Value.m_connectingEdge != null)
					{
						closedEdges.Add(closedNode.Value.m_connectingEdge);
					}
				}
			}

			SEL_debug.CreateRouteQueryDebugMap(source, destination, closedVertices, closedEdges, SELConfig.Instance.RouteFinderOutputResolution());
		}
	}
}
