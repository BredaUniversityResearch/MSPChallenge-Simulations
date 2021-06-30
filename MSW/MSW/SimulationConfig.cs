using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace MSW
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class SimulationConfig
	{
		[JsonProperty]
		private string simulation_name = null;
		public string SimulationName => simulation_name;

		[JsonProperty]
		private string relative_exe_path = null;
		public string RelativeExePath => relative_exe_path;
	}
}
