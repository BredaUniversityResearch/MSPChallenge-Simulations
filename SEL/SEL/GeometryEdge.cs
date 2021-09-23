using SEL.SpatialMapping;
using System;

namespace SEL
{
	/// <summary>
	/// Base class for an edge connecting two geometry vertices.
	/// </summary>
	public class GeometryEdge
	{
		public readonly GeometryVertex m_from;
		public readonly GeometryVertex m_to;
		public readonly double m_distance;

		public GeometryEdge(GeometryVertex from, GeometryVertex to)
		{
			if (from == to)
				throw new ArgumentException("Can't connect a vertex with itself!");

			m_from = from;
			m_to = to;
			m_distance = (from.position - to.position).Magnitude();
		}

		public bool IntersectionTest(GeometryEdge otherEdge, out float otherEdgeIntersectionTime)
		{
			// https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect

			Vector2D thisLine = m_to.position - m_from.position;
			Vector2D otherLine = otherEdge.m_to.position - otherEdge.m_from.position;
			Vector2D deltaFromPoints = (otherEdge.m_from.position - m_from.position);

			double startPointCrossResult = deltaFromPoints.CrossProduct(thisLine);
			if (Math.Abs(startPointCrossResult) < double.Epsilon)
			{
				//Lines are collinear and intersect if they have any overlap.
				otherEdgeIntersectionTime = 0.5f;
				return ((otherEdge.m_from.position.x - m_from.position.x < 0.0) != (otherEdge.m_from.position.x - m_to.position.x < 0.0)) ||
						((otherEdge.m_from.position.y - m_from.position.y < 0.0) != (otherEdge.m_from.position.y - m_to.position.y < 0.0));
			}

			double crossResult = thisLine.CrossProduct(otherLine);
			if (Math.Abs(crossResult) < double.Epsilon)
			{
				otherEdgeIntersectionTime = 0.0f;
				return false; //parallel lines, no intersection.
			}

			double rcpCrossResult = 1.0f / crossResult;
			double thisLineIntersectionTime = deltaFromPoints.CrossProduct(otherLine) * rcpCrossResult;
			double otherLineIntersectionTime = deltaFromPoints.CrossProduct(thisLine) * rcpCrossResult;

			otherEdgeIntersectionTime = (float)otherLineIntersectionTime;
			return (thisLineIntersectionTime >= 0.0 && thisLineIntersectionTime <= 1.0 && otherLineIntersectionTime >= 0.0 && otherLineIntersectionTime <= 1.0);
		}

		public AABB CalculateBounds()
		{
			Vector2D min = Vector2D.Min(m_from.position, m_to.position);
			Vector2D max = Vector2D.Max(m_from.position, m_to.position);
			return new AABB(min, max);
		}
	}
}
