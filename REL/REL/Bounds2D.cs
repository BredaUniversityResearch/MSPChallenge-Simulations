using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REL
{
	public struct Bounds2D
	{
		public readonly Vector2D Min;
		public readonly Vector2D Max;
		public readonly double Width;
		public readonly double Height;

		public Bounds2D(Vector2D a_min, Vector2D a_max)
		{
			Min = a_min;
			Max = a_max;
			Width = Max.x - Min.x;
			Height = Max.y - Min.y;
		}

		public double[][] ToArray()
		{
			return new double[2][] { new double[2] { Min.x, Min.y}, new double[2]{ Max.x, Max.y} };
		}
	}
}
