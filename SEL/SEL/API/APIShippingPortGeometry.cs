using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace SEL.API
{
	/// <summary>
	/// Port geometry as defined by the API. 
	/// Each port geometry represents a single point and it's geometry is defined by a number of points that make up a polygon. 
	/// </summary>
	public class APIShippingPortGeometry
	{
		public string port_id = "";
		public double[][] geometry = null;
		public int geometry_persistent_id;
		public EShippingPortType port_type;
		public int construction_start_time = 0;
		public int construction_end_time = 0;

		[JsonIgnore]
		public Vector2D center { get; private set; }

		[OnDeserialized]
		protected void OnPostDeserialization(StreamingContext context)
		{
			Vector2D centerPoint = new Vector2D();
			foreach (double[] portGeometryPoint in geometry)
			{
				centerPoint += new Vector2D(portGeometryPoint[0], portGeometryPoint[1]);
			}

			center = centerPoint / (double)geometry.Length; 
		}
	}
}