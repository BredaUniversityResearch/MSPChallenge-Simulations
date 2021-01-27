using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEL.API
{
	/// <summary>
	/// A configuration setting that specifies how many boats need to go from source to destination. 
	/// These intensities are configured in percentages of the total amount of ship calls that are generated in the source port.
	/// The remainder of the ship calls will be distributed over all the other destinations depending on the destination ship intensity.
	/// </summary>
	public class APIConfiguredIntensityRoute
	{
		public string source_port_id;
		public string destination_port_id;
		public int ship_type_id;
		public float intensity_percentage;
		public int start_time = 0;
	}
}
