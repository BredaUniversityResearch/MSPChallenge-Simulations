using SEL.SpatialMapping;

namespace SEL.API
{
	internal class APIBounds
	{
		public double x_min;
		public double y_min;
		public double x_max;
		public double y_max;

		public AABB ToAABB()
		{
			return new AABB(new Vector2D(x_min, y_min), new Vector2D(x_max, y_max));
		}

		public double GetWidth()
		{
			return x_max - x_min;
		}

		public double GetHeight()
		{
			return y_max - y_min;
		}
	}
}