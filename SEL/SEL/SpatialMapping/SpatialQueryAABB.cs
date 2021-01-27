using System;

namespace SEL.SpatialMapping
{
	/// <summary>
	/// Spatial query representing an bounding box.
	/// Returns all entities that overlap the bounds given to this query.
	/// </summary>
	public class SpatialQueryAABB
	{
		public AABB bounds { get; private set; }

		public SpatialQueryAABB(AABB a_bounds)
		{
			bounds = a_bounds;
		}

		public SpatialQueryAABB(Vector2D min, Vector2D max)
		{
			bounds = new AABB(min, max);
		}

		public bool CheckBounds(AABB a_bounds)
		{
			return bounds.IntersectsWith(a_bounds);
		}

		public IQuadTreeNodeSelector GetNodeSelector()
		{
			return new QuadTreeNodeSelectorAABB(bounds);
		}
	}
}
