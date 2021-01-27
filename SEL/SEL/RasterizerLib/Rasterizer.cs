using System;
using System.Collections.Generic;
using System.Numerics;
using Poly2Tri.Triangulation.Delaunay;
using Poly2Tri.Triangulation.Delaunay.Sweep;
using Poly2Tri.Triangulation.Polygon;

namespace SEL.RasterizerLib
{
	static class Rasterizer
	{
		private const float intConversion = 100.0f;
		
		public static void RasterizePolygonsFlat(RasterizerSurface targetSurface, RestrictionMesh mesh, int valueToSet, Rect rasterBounds)
		{
			PolygonRasterizer.DrawPolygons(targetSurface, valueToSet, mesh.m_rasterSpaceTriangulatedMesh);
		}

		public static void RasterizeMesh(RestrictionMesh restrictionMesh, List<DelaunayTriangle> outputList)
		{
			List<PolygonPoint> polygonPoints = new List<PolygonPoint>(restrictionMesh.m_lineGeometry.Length);
			foreach (double[] linePoint in restrictionMesh.m_lineGeometry)
			{
				polygonPoints.Add(new PolygonPoint(linePoint[0], linePoint[1]));
			}

			Polygon polygon = new Polygon(polygonPoints);
			DTSweepContext tcx = new DTSweepContext();
			tcx.PrepareTriangulation(polygon);
			DTSweep.Triangulate(tcx);

			int requiredListCapacity = outputList.Count + polygon.Triangles.Count;
			if (outputList.Capacity < requiredListCapacity)
			{
				outputList.Capacity = requiredListCapacity;
			}

			outputList.AddRange(polygon.Triangles);
		}

		public static void RasterizeMeshRescaleToBounds(RestrictionMesh restrictionMesh, Rect rasterBounds, Vector2 rasterResolution, List<DelaunayTriangle> outputList)
		{
			long shiftedXMin = (long)(rasterBounds.xMin * intConversion);
			long shiftedYMin = (long)(rasterBounds.yMin * intConversion);
			long shiftedXMax = (long)(rasterBounds.xMax * intConversion);
			long shiftedYMax = (long)(rasterBounds.yMax * intConversion);

			List<IntPoint> intPoly = VectorToIntPoint(restrictionMesh.m_lineGeometry);

			Clipper clipper = new Clipper();

			clipper.AddPaths(new List<List<IntPoint>>() { intPoly }, PolyType.ptSubject, true);
			List<List<IntPoint>> clipPoly =
				new List<List<IntPoint>>() { GetSquarePoly(shiftedXMin, shiftedXMax, shiftedYMin, shiftedYMax) };
			clipper.AddPaths(clipPoly, PolyType.ptClip, true);

			List<List<IntPoint>> clippingSpaceMeshes = new List<List<IntPoint>>();

			clipper.Execute(ClipType.ctIntersection, clippingSpaceMeshes, PolyFillType.pftNonZero);

			foreach (List<IntPoint> clippingSpaceMesh in clippingSpaceMeshes)
			{
				List<PolygonPoint> screenSpaceVertices = PolygonRasterizer.TransformToRasterSpace(clippingSpaceMesh, shiftedXMin, shiftedXMax,
					shiftedYMin, shiftedYMax, rasterResolution);

				Polygon polygon = new Polygon(screenSpaceVertices);
				DTSweepContext tcx = new DTSweepContext();
				tcx.PrepareTriangulation(polygon);
				DTSweep.Triangulate(tcx);

				int requiredListCapacity = outputList.Count + polygon.Triangles.Count;
				if (outputList.Capacity < requiredListCapacity)
				{
					outputList.Capacity = requiredListCapacity;
				}

				outputList.AddRange(polygon.Triangles);
			}
		}

		private static List<IntPoint> VectorToIntPoint(IEnumerable<double[]> points)
		{
			List<IntPoint> verts = new List<IntPoint>();
			foreach (double[] point in points)
			{
				verts.Add(new IntPoint(point[0] * intConversion, point[1] * intConversion));
			}
			return verts;
		}

		private static List<IntPoint> GetSquarePoly(long xMin, long xMax, long yMin, long yMax)
		{
			return new List<IntPoint>()
			{
				new IntPoint(xMin, yMin),
				new IntPoint(xMin, yMax),
				new IntPoint(xMax, yMax),
				new IntPoint(xMax, yMin)
			};
		}

		private static double GetPolygonArea(List<List<IntPoint>> polygons)
		{
			double area = 0;
			foreach (List<IntPoint> polygon in polygons)
			{
				for (int i = 0; i < polygon.Count; ++i)
				{
					int j = (i + 1) % polygon.Count;
					area += polygon[i].Y * polygon[j].X - polygon[i].X * polygon[j].Y;
				}
			}
			return Math.Abs(area * 0.5);
		}
	}
}
