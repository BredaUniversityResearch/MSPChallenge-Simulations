using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace MSW
{
	public class MswConfig
	{
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private class JsonData
		{
			public SimulationConfig[] available_simulations = null;
		}

		private static MswConfig _msInstance = null;
		public static MswConfig Instance => _msInstance;

		private JsonData m_settings = null;

		static MswConfig()
		{
			_msInstance = new MswConfig();
		}

		private MswConfig()
		{
			string configString = File.ReadAllText("MSW_config.json", Encoding.UTF8);
			m_settings = JsonConvert.DeserializeObject<JsonData>(configString);
		}

		public SimulationConfig GetSimulationConfigForType(string a_simulationType)
		{
			SimulationConfig config =
				Array.Find(m_settings.available_simulations, a_obj => a_obj.SimulationName == a_simulationType);
			if (config == null)
			{
				throw new Exception("Unknown simulation type " + a_simulationType);
			}

			return config;
		}

		public IEnumerable<SimulationConfig> GetAllSimulationConfig()
		{
			return m_settings.available_simulations;
		}
	}
}
