using System;

namespace REL
{
	class HeatmapCoordinateConverter
	{
		private MarinGridDefinition m_gridDefinition;
		public readonly Vector2D OutputAreaTopLeftMspSpace;
		public readonly Vector2D OutputAreaTopRightMspSpace;
		public readonly Vector2D OutputAreaBottomLeftMspSpace;
		public readonly Vector2D OutputAreaBottomRightMspSpace;

		public readonly int OutputPixelWidth;
		public readonly int OutputPixelHeight;

		public HeatmapCoordinateConverter(MarinGridDefinition a_gridDefinition)
		{
			m_gridDefinition = a_gridDefinition;
			OutputAreaTopLeftMspSpace = a_gridDefinition.GetGridPositionMspSpace(a_gridDefinition.GridXMin, a_gridDefinition.GridYMin);
			OutputAreaTopRightMspSpace = a_gridDefinition.GetGridPositionMspSpace(a_gridDefinition.GridXMax, a_gridDefinition.GridYMin);
			OutputAreaBottomLeftMspSpace = a_gridDefinition.GetGridPositionMspSpace(a_gridDefinition.GridXMin, a_gridDefinition.GridYMax);
			OutputAreaBottomRightMspSpace = a_gridDefinition.GetGridPositionMspSpace(a_gridDefinition.GridXMax, a_gridDefinition.GridYMax);

			RectifyGrid(ref OutputAreaTopLeftMspSpace, ref OutputAreaTopRightMspSpace, ref OutputAreaBottomLeftMspSpace, ref OutputAreaBottomRightMspSpace);

			double projectedMaxWidth = Math.Max(OutputAreaTopRightMspSpace.x - OutputAreaTopLeftMspSpace.x, OutputAreaBottomRightMspSpace.x - OutputAreaBottomLeftMspSpace.x);
			double projectedMaxHeight = Math.Max(OutputAreaTopLeftMspSpace.y - OutputAreaBottomLeftMspSpace.y, OutputAreaTopRightMspSpace.y - OutputAreaBottomRightMspSpace.y);

			double gridOutputScaleWidth = m_gridDefinition.GetMspSpaceBounds().Width / projectedMaxWidth;
			double gridOutputScaleHeight = m_gridDefinition.GetMspSpaceBounds().Height / projectedMaxHeight;
			if (gridOutputScaleWidth < 1.0 || gridOutputScaleHeight < 1.0)
			{
				throw new Exception("Got < 1.0 scale for output grid");
			}
			OutputPixelWidth = (int)Math.Ceiling((double)(a_gridDefinition.GridXMax - a_gridDefinition.GridXMin) * gridOutputScaleWidth);
			OutputPixelHeight = (int)Math.Ceiling((double)(a_gridDefinition.GridYMax - a_gridDefinition.GridYMin) * gridOutputScaleHeight);
		}

		public Vector2D ToRasterPosition(Vector2D a_mspPosition)
		{
			Bounds2D fullImageBounds = m_gridDefinition.GetMspSpaceBounds();
			Vector2D relativePosition = a_mspPosition - fullImageBounds.Min;
			double u = relativePosition.x / fullImageBounds.Width;
			double v = relativePosition.y / fullImageBounds.Height;

			if (u < 0 || u > 1.0 || v < 0 || v > 1.0)
			{
				throw new Exception();
			}

			return new Vector2D(u * (OutputPixelWidth - 1), v * (OutputPixelHeight - 1));
		}

		//X [0..1], Y [0..1]
		public Vector2D ToNormalizedOutputGridPosition(Vector2D a_mspPosition)
		{
			Vector2D barycentric = GetBarycentricCoordinates(a_mspPosition, OutputAreaTopLeftMspSpace, OutputAreaBottomLeftMspSpace,
				OutputAreaBottomRightMspSpace);
			if (!IsWithinTriangle(barycentric))
			{
				barycentric = GetBarycentricCoordinates(a_mspPosition, OutputAreaTopLeftMspSpace, OutputAreaTopRightMspSpace,
					OutputAreaBottomRightMspSpace);
				if (!IsWithinTriangle(barycentric))
				{
					throw new ArgumentOutOfRangeException(nameof(a_mspPosition), "Point is not within grid position");
				}
			}

			return new Vector2D(barycentric.x, barycentric.y);
		}

		//Returns u,v coordinates of lines C-A and B-A.
		//WorldPos = A + (((C - A) * u) + (B - A) * v));
		private Vector2D GetBarycentricCoordinates(Vector2D a_point, Vector2D a_trianglePointA, 
			Vector2D a_trianglePointB, Vector2D a_trianglePointC)
		{
			//https://observablehq.com/@kelleyvanevert/2d-point-in-triangle-test
			Vector2D AC = a_trianglePointC - a_trianglePointA;
			Vector2D AB = a_trianglePointB - a_trianglePointA;
			Vector2D AP = a_point - a_trianglePointA;

			double ACdotAC = AC.DotProduct(AC);
			double ABdotAC = AB.DotProduct(AC);
			double ACdotAP = AC.DotProduct(AP);
			double ABdotAB = AB.DotProduct(AB);
			double ABdotAP = AB.DotProduct(AP);

			double denom = (ACdotAC * ABdotAB) - (ABdotAC * ABdotAC);
			double u = ((ABdotAB * ACdotAP) - (ABdotAC * ABdotAP)) / denom;
			double v = ((ACdotAC * ABdotAP) - (ABdotAC * ACdotAP)) / denom;

			return new Vector2D(u, v);
		}

		private bool IsWithinTriangle(Vector2D a_barycentricCoordinates)
		{
			return (a_barycentricCoordinates.x >= 0.0) && a_barycentricCoordinates.y >= 0.0 &&
			       (a_barycentricCoordinates.x + a_barycentricCoordinates.y <= 1.0);
		}

		private void RectifyGrid(ref Vector2D a_topLeft, ref Vector2D a_topRight, ref Vector2D a_bottomLeft, ref Vector2D a_bottomRight)
		{
			EnsureLessThan(ref a_topLeft.x, ref a_topRight.x);
			EnsureLessThan(ref a_bottomLeft.x, ref a_bottomRight.x);
			EnsureLessThan(ref a_bottomLeft.y, ref a_topLeft.y);
			EnsureLessThan(ref a_bottomRight.y, ref a_topRight.y);
		}

		private static void EnsureLessThan(ref double a_value, ref double a_lessThan)
		{
			if (a_value > a_lessThan)
			{
				double t = a_value;
				a_value = a_lessThan;
				a_lessThan = t;
			}
		}
	};
}