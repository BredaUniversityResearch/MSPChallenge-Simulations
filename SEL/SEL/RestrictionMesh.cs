using System;
using System.Collections.Generic;
using Poly2Tri.Triangulation;
using Poly2Tri.Triangulation.Delaunay;
using SEL.RasterizerLib;
using SEL.SpatialMapping;

namespace SEL
{
	/// <summary>
	/// Container class that holds a number 
	/// </summary>
	class RestrictionMesh
	{
		public readonly int m_geometryId;
		public readonly int m_layerId;
		public readonly RestrictionGeometryType m_restrictionType;
		public readonly double[][] m_lineGeometry;

		public readonly AABB m_bounds;
		public List<DelaunayTriangle> m_rasterSpaceTriangulatedMesh;
		private QuadTree<DelaunayTriangle> m_worldSpaceTriangulatedMesh = null;

		public RestrictionMesh(int geometryId, int layerId, RestrictionGeometryType restrictionType, double[][] lineGeometry)
		{
			m_geometryId = geometryId;
			m_layerId = layerId;
			m_restrictionType = restrictionType;
			m_lineGeometry = lineGeometry;
			m_rasterSpaceTriangulatedMesh = new List<DelaunayTriangle>(lineGeometry.Length / 3); //Probably way off but gives us an indication...

			Vector2D boundsMin = new Vector2D(1e25f, 1e25f);
			Vector2D boundsMax = new Vector2D(0.0f, 0.0f);
			foreach (double[] geometryPoint in lineGeometry)
			{
				boundsMin.x = Math.Min(boundsMin.x, geometryPoint[0]);
				boundsMin.y = Math.Min(boundsMin.y, geometryPoint[1]);
				boundsMax.x = Math.Max(boundsMax.x, geometryPoint[0]);
				boundsMax.y = Math.Max(boundsMax.y, geometryPoint[1]);
			}

			m_bounds = new AABB(boundsMin, boundsMax);
		}

		public void SetWorldSpaceTriangulatedMesh(List<DelaunayTriangle> triangles)
		{
			m_worldSpaceTriangulatedMesh = new QuadTree<DelaunayTriangle>(m_bounds);
			foreach (DelaunayTriangle triangle in triangles)
			{
				Vector2D min = new Vector2D(1e25f, 1e25f);
				Vector2D max = new Vector2D(-1e25f, -1e25f);

				for (int i = 0; i < 3; ++i)
				{
					min.x = Math.Min(min.x, triangle.Points[i].X);
					min.y = Math.Min(min.y, triangle.Points[i].Y);
					max.x = Math.Max(max.x, triangle.Points[i].X);
					max.y = Math.Max(max.y, triangle.Points[i].Y);
				}

				m_worldSpaceTriangulatedMesh.Insert(new AABB(min, max), triangle);
			}
		}

		public bool ContainsPoint(Vector2D worldSpacePosition)
		{
			if (m_worldSpaceTriangulatedMesh.GetPopulatedNodeCount() == 0)
			{
				throw new Exception("Tried to get point on restriction mesh that is not triangulated yet.");
			}

			SpatialQueryAABB query = new SpatialQueryAABB(worldSpacePosition, worldSpacePosition);
			foreach (DelaunayTriangle triangle in m_worldSpaceTriangulatedMesh.Query(query))
			{
				if (TriangleContainsPoint(triangle, worldSpacePosition))
				{
					return true;
				}
			}

			return false;
		}

		//https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle
		private double CalculateSideOfEdge(double x1, double y1, double x2, double y2, double x3, double y3)
		{
			return (x1 - x3) * (y2 - y3) - (x2 - x3) * (y1 - y3);
		}

		private bool TriangleContainsPoint(DelaunayTriangle triangle, Vector2D position)
		{
			double d1 = CalculateSideOfEdge(position.x, position.y, triangle.Points[0].X, triangle.Points[0].Y, triangle.Points[1].X, triangle.Points[1].Y);
			double d2 = CalculateSideOfEdge(position.x, position.y, triangle.Points[1].X, triangle.Points[1].Y, triangle.Points[2].X, triangle.Points[2].Y);
			double d3 = CalculateSideOfEdge(position.x, position.y, triangle.Points[2].X, triangle.Points[2].Y, triangle.Points[0].X, triangle.Points[0].Y);

			bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
			bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

			return !(hasNeg && hasPos);
		}
	}
}
