using System.Collections.Generic;

namespace SEL.API
{
	/// <summary>
	/// Shipping lane geometry representation 
	/// Each instance contains a list of points that represents a single shipping lane geometry
	/// The points are implicitly connected via edges to each successive point. (e.g. 0 -> 1 -> 2 -> 3)
	/// </summary>
	class APIShippingLaneGeometry: APIGeometryData
	{
		public int[] ship_type_ids = null; //List of ship type ids that can traverse this lane. If none are specified we assume all ships can traverse the lane.
		public Dictionary<string, string> geometry_data = new Dictionary<string, string>();
	}
}
