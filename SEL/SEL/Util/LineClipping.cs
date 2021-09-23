using SEL.SpatialMapping;
using System;
using System.Drawing;
using System.Numerics;

namespace SEL.Util
{
	public static class LineClipping
	{

		[Flags]
		private enum EClipOutCode
		{
			Inside = 0,
			Left = (1 << 0),
			Right = (1 << 1),
			Bottom = (1 << 2),
			Top = (1 << 3)
		};

		/// <summary>
		/// Clips lines via the Cohen Sutherland algorithm and puts the clipped points 
		/// back into the from and to parameters.
		/// Returns false when no line needs to be drawn due to it all being out of bounds. 
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="bounds"></param>
		public static bool ClipLinePoints(ref Point from, ref Point to, AABB bounds)
		{
			Vector2 fromVec = new Vector2((float)from.X, (float)from.Y);
			Vector2 toVec = new Vector2((float)to.X, (float)to.Y);

			bool isValidLine = ClipLinePoints(ref fromVec, ref toVec, bounds);

			from.X = (int)fromVec.X;
			from.Y = (int)fromVec.Y;
			to.X = (int)toVec.X;
			to.Y = (int)toVec.Y;

			return isValidLine;
		}

		private static bool ClipLinePoints(ref Vector2 from, ref Vector2 to, AABB bounds)
		{
			EClipOutCode fromClipCode = ComputeOutCode(from, bounds);
			EClipOutCode toClipCode = ComputeOutCode(to, bounds);
			bool validLine = false;

			while (true)
			{
				if ((fromClipCode | toClipCode) == 0)
				{
					//Both the From and To points are inside ((Inside | Inside) == 0), accept the line because we don't need anything else anymore.
					validLine = true;
					break;
				}
				else if ((fromClipCode & toClipCode) != 0)
				{
					//Both points are outside of the clipping bounds and on the same side of the clipping bounds (e.i. (Left & Left) != 0. Reject the line and return.
					break;
				}

				EClipOutCode codeToProcess = (fromClipCode != EClipOutCode.Inside) ? fromClipCode : toClipCode;
				Vector2 clippedPoint = new Vector2();

				if ((codeToProcess & EClipOutCode.Top) != 0)
				{
					clippedPoint.X = from.X + (to.X - from.X) * ((float)bounds.max.y - from.Y - 1.0f) / (to.Y - from.Y);
					clippedPoint.Y = (float)bounds.max.y - 1.0f;
				}
				else if ((codeToProcess & EClipOutCode.Bottom) != 0)
				{
					clippedPoint.X = from.X + (to.X - from.X) * ((float)bounds.min.y - from.Y) / (to.Y - from.Y);
					clippedPoint.Y = (float)bounds.min.y;
				}
				else if ((codeToProcess & EClipOutCode.Right) != 0)
				{
					clippedPoint.Y = from.Y + (to.Y - from.Y) * ((float)bounds.max.x - from.X - 1.0f) / (to.X - from.X);
					clippedPoint.X = (float)bounds.max.x - 1.0f;
				}
				else if ((codeToProcess & EClipOutCode.Left) != 0)
				{
					clippedPoint.Y = from.Y + (to.Y - from.Y) * ((float)bounds.min.x - from.X) / (to.X - from.X);
					clippedPoint.X = (float)bounds.min.x;
				}

				//Put the result back.
				if (codeToProcess == fromClipCode)
				{
					from = clippedPoint;
					fromClipCode = ComputeOutCode(from, bounds);
				}
				else
				{
					to = clippedPoint;
					toClipCode = ComputeOutCode(to, bounds);
				}
			}

			return validLine;
		}

		private static EClipOutCode ComputeOutCode(Vector2 point, AABB bounds)
		{
			EClipOutCode result = EClipOutCode.Inside;
			if (point.X < bounds.min.x)
			{
				result |= EClipOutCode.Left;
			}
			else if (point.X >= bounds.max.x)
			{
				result |= EClipOutCode.Right;
			}

			if (point.Y < bounds.min.y)
			{
				result |= EClipOutCode.Bottom;
			}
			else if (point.Y >= bounds.max.y)
			{
				result |= EClipOutCode.Top;
			}

			return result;
		}
	}
}
