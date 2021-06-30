using System.Collections.Generic;

namespace SEL
{
	/// <summary>
	/// A 'complete' representation of a shipping lane. 
	/// Contains a string of edges that make up a shipping lane for a particular geometry id
	/// Edges are contained in a list that is sorted from start -> end
	/// </summary>
	class LaneEdgeString
	{
		private List<LaneEdge> m_edgeList = new List<LaneEdge>();

		public int laneGeometryId { get; private set; }

		public LaneEdgeString(int a_laneGeometryId)
		{
			laneGeometryId = a_laneGeometryId;
		}

		public void AddNextEdge(LaneEdge edge)
		{
			m_edgeList.Add(edge);
		}

		public LaneVertex GetFirstVertex()
		{
			return m_edgeList[0].typedFrom;
		}

		public LaneVertex GetLastVertex()
		{
			return m_edgeList[m_edgeList.Count - 1].typedTo;
		}
	}
}
