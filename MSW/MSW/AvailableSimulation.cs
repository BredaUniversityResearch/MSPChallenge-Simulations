using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MSW
{
	public class AvailableSimulation
	{
		public const string LatestVersionName = "1.0.0";

		private class SimulationVersion
		{
			public string ExePath;
			public string Version;
		};

		private readonly SimulationConfig m_config;

		private List<SimulationVersion> m_availableVersions = new List<SimulationVersion>();
		public string SimulationType => m_config.SimulationName;

		public AvailableSimulation(SimulationConfig a_config)
		{
			m_config = a_config;
			DiscoverAvailableVersions();
		}

		private void DiscoverAvailableVersions()
		{
			string version = LatestVersionName;
			string versionFile = m_config.SimulationName + @"data\version.txt";
			if (File.Exists(versionFile))
			{
				string contents = File.ReadAllText(versionFile);
				Regex reVersion = new Regex("([0-9.]+)");
				Match reVersionMatch = reVersion.Match(contents);
				if (reVersionMatch.Success)
				{
					version = reVersionMatch.Value;
				}
				else
				{
					ConsoleLogger.Warning(m_config.SimulationName + @"data\version.txt file found, but version in it seems to be of wrong format. Make sure it's something like 1.0.0");
				}
			}
			else
			{
				ConsoleLogger.Warning(m_config.SimulationName + @"data\version.txt file not found, so no version could be determined.");
			}
			ConsoleLogger.Info("Registering " + m_config.SimulationName + " version " + version);
			m_availableVersions.Add(new SimulationVersion {ExePath = m_config.RelativeExePath, Version = version});

			VerifyVersionConfigurations();
		}

		private void VerifyVersionConfigurations()
		{
			foreach (SimulationVersion availableSim in m_availableVersions)
			{
				if (!File.Exists(availableSim.ExePath))
				{
					ConsoleLogger.Error($"Simulation executable at {availableSim.ExePath} does not exist");
				}
			}
		}

		public AvailableSimulationVersion GetSimulationVersion(string a_version)
		{
			if (string.IsNullOrEmpty(a_version))
			{
				return GetLatestSimulationVersion();
			}
			return new AvailableSimulationVersion(this, a_version);
		}

		public string GetExecutablePathForVersion(string a_requestedVersion)
		{
			SimulationVersion version = m_availableVersions.Find(obj => obj.Version.Equals(a_requestedVersion, StringComparison.InvariantCultureIgnoreCase));
			if (version == null)
			{
				if (!a_requestedVersion.Equals(LatestVersionName, StringComparison.InvariantCultureIgnoreCase))
				{
					ConsoleLogger.Warning("Requested simulation version \"" + a_requestedVersion + "\" for simulation type \"" + SimulationType + "\" which is not known. Falling back to latest version available");
				}
				return GetExecutablePathForVersion(m_availableVersions[m_availableVersions.Count - 1].Version);
			}

			return version.ExePath;
		}

		public AvailableSimulationVersion GetLatestSimulationVersion()
		{
			return GetSimulationVersion(m_availableVersions[m_availableVersions.Count - 1].Version);
		}

		public bool HasVersionAvailable(string a_simulationRequestSimulationVersion)
		{
			//If no version is explicitly specified fall back to the latest version.
			if (string.IsNullOrEmpty(a_simulationRequestSimulationVersion) || a_simulationRequestSimulationVersion.Equals(LatestVersionName, StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}
			return m_availableVersions.Find(a_obj => a_obj.Version == a_simulationRequestSimulationVersion) != null;
		}
	}
}
