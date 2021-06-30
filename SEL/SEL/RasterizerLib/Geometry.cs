using System.Collections.Generic;

namespace SEL.RasterizerLib
{
	public class Geometry
	{
		public string geotype { get; set; }
		public List<List<double[]>> geometry { get; set; }
	}
}
