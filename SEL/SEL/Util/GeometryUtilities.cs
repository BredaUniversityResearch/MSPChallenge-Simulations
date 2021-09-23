namespace SEL
{
	static class GeometryUtilities
	{
		public static double[][] CreateSquareGeometryDataCenteredAt(Vector2D centerPosition, Vector2D halfExtents)
		{
			double[][] result = new double[5][];

			Vector2D topLeft = centerPosition - halfExtents;
			Vector2D bottomRight = centerPosition + halfExtents;

			result[0] = new[] {topLeft.x, topLeft.y};
			result[1] = new[] {bottomRight.x, topLeft.y};
			result[2] = new[] {bottomRight.x, bottomRight.y};
			result[3] = new[] {topLeft.x, bottomRight.y};
			result[4] = new[] { topLeft.x, topLeft.y };

			return result;
		}
	}
}