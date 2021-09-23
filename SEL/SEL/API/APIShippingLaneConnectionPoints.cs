namespace SEL.API
{
	/// <summary>
	/// Separate vertices that define connection points between two geometry lines
	/// Essentially geometry with a single vertex which we can use to identify connection points.
	/// </summary>
	public class APIShippingLaneConnectionPoints
	{
		public int geometry_id;
		public double[] geometry;
	}
}
