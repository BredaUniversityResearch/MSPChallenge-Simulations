using System;

namespace SEL.SpatialMapping
{
	/// <summary>
	/// Bounding box representation for use with the spatial mapping system.
	/// </summary>
	public struct AABB
	{
		public Vector2D min { get; private set; }
		public Vector2D max { get; private set; }

		public AABB(Vector2D a_min, Vector2D a_max)
		{
			min = a_min;
			max = a_max;
		}

		public EIntersectResult IntersectTest(AABB bounds)
		{
			if (Contains(bounds))
			{
				return EIntersectResult.Contained;
			}

			if (IntersectsWith(bounds))
			{
				return EIntersectResult.Intersects;
			}

			return EIntersectResult.NoIntersection;
		}

		public bool IntersectsWith(AABB bounds)
		{
			if (max.x < bounds.min.x || min.x > bounds.max.x)
				return false;
			if (max.y < bounds.min.y || min.y > bounds.max.y)
				return false;
			return true;
		}

		public bool Contains(AABB bounds)
		{
			return (bounds.min.x >= min.x && bounds.max.x <= max.x &&
					bounds.min.y >= min.y && bounds.max.y <= max.y);
		}
	}
}
