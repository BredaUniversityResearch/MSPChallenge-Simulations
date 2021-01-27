using System;

namespace SEL.RasterizerLib
{
	public class RasterizerSurface
	{
		public readonly int Width;
		public readonly int Height;

		public int[] Values
		{
			get;
			private set;
		}

		public RasterizerSurface(int width, int height, int[] referenceValues = null)
		{
			Width = width;
			Height = height;
			if (referenceValues == null)
			{
				Values = new int[width * height];
			}
			else
			{
				Values = referenceValues;
			}
		}

		public int GetValueAt(int x, int y)
		{
			if (x < 0 || x >= Width)
			{
				throw new ArgumentOutOfRangeException("x");
			}

			if (y < 0 || y > Height)
			{
				throw new ArgumentOutOfRangeException("y");
			}

			return Values[x + (y * Width)];
		}

		public bool TrySetValueAt(int x, int y, int value)
		{
			if (x >= 0 && x < Width && y >= 0 && y < Height)
			{
				Values[x + (y * Width)] = value;
				return true;
			}

			return false;
		}
	}
}
