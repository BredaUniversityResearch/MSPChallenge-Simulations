namespace SEL
{
	/// <summary>
	/// Base vertex representation. Basically a point and some tracking info.
	/// </summary>
	public class GeometryVertex
	{
		private static int s_vertexCounter = 0;

		public Vector2D position = new Vector2D();
		public int vertexId { get; }

		public GeometryVertex(double x, double y)
		{
			position.x = x;
			position.y = y;
			vertexId = s_vertexCounter++;
		}
	}
}
