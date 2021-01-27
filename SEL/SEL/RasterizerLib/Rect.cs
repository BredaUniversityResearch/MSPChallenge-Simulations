
namespace SEL.RasterizerLib
{
	struct Rect
	{
		public float x;
		public float y;
		public float xmax;
		public float ymax;

		public float xMin
		{
			get
			{
				return x;
			}
		}

		public float yMin
		{
			get
			{
				return y;
			}
		}

		public float xMax
		{
			get
			{
				return xmax;
			}
		}

		public float yMax
		{
			get
			{
				return ymax;
			}
		}

		public Rect(float x, float y, float xmax, float ymax)
		{
			this.x = x;
			this.y = y;
			this.xmax = xmax;
			this.ymax = ymax;
		}
	}
}