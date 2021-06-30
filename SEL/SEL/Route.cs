using System.Collections.Generic;

namespace SEL
{
	/// <summary>
	/// Route defines a path from vertex X to vertex Y using a set of edges.
	/// A route is specialised towards a single ship type since they all might take different routes.
	/// </summary>
	class Route
	{
		private List<LaneEdge> m_edgeList = new List<LaneEdge>();
		public readonly ShipType ShipTypeInfo;
		public readonly LaneVertex FromVertex;
		public readonly LaneVertex ToVertex;

		public ERouteDirectionality Directionality
		{
			get;
			private set;
		}

		public Route(LaneVertex fromVertex, LaneVertex toVertex, ShipType shipTypeInfo)
		{
			FromVertex = fromVertex;
			ToVertex = toVertex;
			ShipTypeInfo = shipTypeInfo;
			Directionality = ERouteDirectionality.Bidirectional;
		}

		public void InsertFirstEdge(LaneEdge edge)
		{
			if (edge.IsUnidirectional())
			{
				Directionality = ERouteDirectionality.Unidirectional;
			}

			m_edgeList.Insert(0, edge);
		}

		public IEnumerable<LaneEdge> GetRouteEdges()
		{
			return m_edgeList;
		}

		public LaneEdge GetFirstLaneEdge()
		{
			return m_edgeList[0];
		}

		public int GetRouteEdgeCount()
		{
			return m_edgeList.Count;
		}
	}
}
