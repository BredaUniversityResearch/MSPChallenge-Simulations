namespace SEL.API
{
	/// <summary>
	/// Restricted zone geometry as defined by the API. 
	/// Each object represents a single restriction zone and it's geometry is defined by a number of points that make up a polygon. 
	/// </summary>
	public class APIShippingRestrictionGeometry: APIGeometryData
	{
		public int layer_id;
		public int[] layer_types;
	}
}