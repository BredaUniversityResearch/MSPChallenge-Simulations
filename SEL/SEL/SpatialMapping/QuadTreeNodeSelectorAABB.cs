using System;

namespace SEL.SpatialMapping
{
	public class QuadTreeNodeSelectorAABB : IQuadTreeNodeSelector
	{
		private AABB m_bounds;

		public QuadTreeNodeSelectorAABB(AABB a_bounds)
		{
			m_bounds = a_bounds;
		}

		public bool EvaluateNode(IQuadTreeNode node)
		{
			return m_bounds.IntersectTest(node.bounds) != EIntersectResult.NoIntersection;
		}
	}
}
