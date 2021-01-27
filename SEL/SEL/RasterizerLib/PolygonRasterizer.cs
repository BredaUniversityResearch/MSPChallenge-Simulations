using System.Collections.Generic;
using System.Numerics;
using Poly2Tri.Triangulation;
using Poly2Tri.Triangulation.Delaunay;
using Poly2Tri.Triangulation.Delaunay.Sweep;
using Poly2Tri.Triangulation.Polygon;

namespace SEL.RasterizerLib
{
	public static class PolygonRasterizer
	{
		public class Raster
		{
			public int m_rasterWidth;
			public int m_rasterHeight;
			public int m_scanlineMin;
			public int m_scanlineMax;
			public ScanlineMinMax[] m_scanlines;

			public void SetupScanlines()
			{
				if (m_scanlines == null || m_scanlines.Length != m_rasterHeight)
				{
					m_scanlines = new ScanlineMinMax[m_rasterHeight];
					for (int i = 0; i < m_scanlines.Length; ++i)
					{
						m_scanlines[i] = new ScanlineMinMax();
					}
					//Setup min & max so it flushes the entire scanline array on the first pass.
					m_scanlineMin = 0;
					m_scanlineMax = m_scanlines.Length - 1;
				}
			}
		}

		public class ScanlineMinMax
		{
			public float xMin;
			public float xMax;
		}

		//This will create a convex outline of the lineMesh given. Convex shapes will be filled up.
		public static void CreateRasterScanlineValues(Raster rasterResult, List<IntPoint> lineMesh, long clippingBoundsMinX, long clippingBoundsMaxX, long clippingBoundsMinY, long clippingBoundsMaxY)
		{
			rasterResult.SetupScanlines();

			ResetScanlines(rasterResult);
			List<PolygonPoint> screenSpaceVertices = TransformToRasterSpace(lineMesh, clippingBoundsMinX, clippingBoundsMaxX, clippingBoundsMinY, clippingBoundsMaxY, new Vector2(rasterResult.m_rasterWidth, rasterResult.m_rasterHeight));

			Polygon polygon = new Polygon(screenSpaceVertices);
			DTSweepContext tcx = new DTSweepContext();
			tcx.PrepareTriangulation(polygon);
			DTSweep.Triangulate(tcx);

			BuildScanlines(rasterResult, polygon.Triangles);
		}
		public static void DrawPolygons(RasterizerSurface targetSurface, int valueToSet, List<DelaunayTriangle> triangles)
		{
			Raster raster = new Raster();
			raster.m_rasterWidth = targetSurface.Width;
			raster.m_rasterHeight = targetSurface.Height;
			raster.SetupScanlines();

			foreach (DelaunayTriangle triangle in triangles)
			{
				ResetScanlines(raster);
				BuildScanlinesForTriangle(raster, triangle);
				RenderScanlines(raster, targetSurface.Values, valueToSet);
			}
		}

		private static void BuildScanlines(Raster raster, IEnumerable<DelaunayTriangle> triangles)
		{
			foreach (DelaunayTriangle triangle in triangles)
			{
				BuildScanlinesForTriangle(raster, triangle);
			}
		}

		private static void BuildScanlinesForTriangle(Raster raster, DelaunayTriangle triangle)
		{
			for (int i = 0; i < 3; ++i)
			{
				TriangulationPoint point0 = triangle.Points[i];
				TriangulationPoint point1 = triangle.Points[(i + 1) % 3];

				Vector2 vertex0 = new Vector2((float)point0.X, (float)point0.Y);
				Vector2 vertex1 = new Vector2((float)point1.X, (float)point1.Y);

				//Swap vertices so we always go from top to bottom with vertex0 to vertex1
				if (vertex0.Y > vertex1.Y)
				{
					Vector2 temp = vertex0;
					vertex0 = vertex1;
					vertex1 = temp;
				}

				float deltaY = vertex1.Y - vertex0.Y;
				if (deltaY == 0.0f)
				{
					//No difference in Y so the line is perfectly horizontal, we can't render that so continue :)
					continue;
				}

				float rcpDeltaY = 1.0f / deltaY; //Multiplication is faster than divide, so precalculate the reciprocal so we can just multiply
				float deltaX = (vertex1.X - vertex0.X) * rcpDeltaY;

				float currentX = vertex0.X;

				//Apply subpixel correction.
				int intY0 = (int)vertex0.Y + 1;
				int intY1 = (int)vertex1.Y;

				float yCorrection = ((float)intY0 - vertex0.Y);
				if (intY0 < 0)
				{
					yCorrection -= vertex1.Y;
					intY0 = 0;
				}

				if (intY1 >= raster.m_rasterHeight)
				{
					intY1 = raster.m_rasterHeight - 1;
				}

				currentX += yCorrection * deltaX;

				//Update the touched scanline bounds.
				if (raster.m_scanlineMin > intY0)
				{
					raster.m_scanlineMin = intY0;
				}
				if (raster.m_scanlineMax < intY1)
				{
					raster.m_scanlineMax = intY1;
				}

				for (int y = intY0; y <= intY1; ++y)
				{
					if (raster.m_scanlines[y].xMin > currentX)
					{
						raster.m_scanlines[y].xMin = currentX;
					}
					if (raster.m_scanlines[y].xMax < currentX)
					{
						raster.m_scanlines[y].xMax = currentX;
					}
					currentX += deltaX;
				}
			}
		}

		/// <summary>
		/// Transforms all vertices to X and Y values from 0 .. width/height within the clipping bounds.
		/// </summary>
		/// <param name="vertices"></param>
		/// <param name="clippingMinX"></param>
		/// <param name="clippingMaxX"></param>
		/// <param name="clippingMinY"></param>
		/// <param name="clippingMaxY"></param>
		/// <returns></returns>
		public static List<PolygonPoint> TransformToRasterSpace(List<IntPoint> vertices, long clippingMinX, long clippingMaxX, long clippingMinY, long clippingMaxY, Vector2 rasterSize)
		{
			double deltaX = (double)(clippingMaxX - clippingMinX);
			double deltaY = (double)(clippingMaxY - clippingMinY);
			List<PolygonPoint> result = new List<PolygonPoint>();
			foreach (IntPoint vertex in vertices)
			{
				double transformedX = ((double)(vertex.X - clippingMinX) / deltaX);
				double transformedY = ((double)(vertex.Y - clippingMinY) / deltaY);
				result.Add(new PolygonPoint(transformedX * rasterSize.X, transformedY * rasterSize.Y));
			}
			return result;
		}

		private static void ResetScanlines(Raster raster)
		{
			for (int i = raster.m_scanlineMin; i <= raster.m_scanlineMax; ++i)
			{
				raster.m_scanlines[i].xMin = 1e25f;
				raster.m_scanlines[i].xMax = -1.0f;
			}
			raster.m_scanlineMax = -1;
			raster.m_scanlineMin = raster.m_rasterHeight + 1;
		}

		private static void RenderScanlines(Raster raster, int[] outputPixels, int valueToSet)
		{
			for (int y = raster.m_scanlineMin; y <= raster.m_scanlineMax; ++y)
			{
				int outputY = y * raster.m_rasterWidth;//(raster.m_rasterHeight - y) * raster.m_rasterWidth;
				ScanlineMinMax scanline = raster.m_scanlines[y];

				for (int x = (int)scanline.xMin; x < (int)scanline.xMax; x++)
				{
					outputPixels[x + outputY] = valueToSet;
				}
			}
		}
	}
}
