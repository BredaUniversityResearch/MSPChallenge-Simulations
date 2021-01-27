using System.Collections.Generic;

namespace MEL
{
	/// <summary>
	/// Pressure configuration as provided by the MSP Platform API
	/// Each pressure has a name and a set of layer data which specifies the influences of these pressures.
	/// </summary>
	public class Pressure
	{
		public string name { get; set; }
		public List<LayerData> layers { get; set; }
	}
}
