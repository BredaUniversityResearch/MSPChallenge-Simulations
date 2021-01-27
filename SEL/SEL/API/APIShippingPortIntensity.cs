namespace SEL.API
{
	/// <summary>
	/// Shipping port intensity for each ship type.
	/// </summary>
	public class APIShippingPortIntensity
	{
		public class ShipTypeIntensity
		{
			public int start_time; //The time from which this intensity counts. 
			public int ship_type_id; //Ship type 
			public int ship_intensity; //The number of ships this port will send out and receive each month.
		}

		public string port_id; //Port's unique ID.
		public ShipTypeIntensity[] ship_intensity_values; 
	}
}