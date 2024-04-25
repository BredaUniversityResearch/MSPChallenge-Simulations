using System.Collections.Generic;

namespace MEL
{
	public class APILayerGeometryData
	{
		public string geotype { get; set; }
		public List<List<List<double[]>>> geometry { get; set; }
		public List<PolicyData> policies = new();
	}
}
