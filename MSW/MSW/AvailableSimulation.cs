using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MSW
{
	public class AvailableSimulation
	{
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
			string versionFolderPattern = "[VERSION_FOLDER]";
			string relativeExePath = m_config.RelativeExePath;
			int basePathLength = relativeExePath.IndexOf(versionFolderPattern, StringComparison.InvariantCulture);
			if (basePathLength == -1)
			{
				ConsoleLogger.Warning("relative_exe_path should be be configured with a " + versionFolderPattern + " expression. Current path is defined as " + 
				                      m_config.RelativeExePath + " for simulation with name " + m_config.SimulationName);
				ConsoleLogger.Warning("Using this as a single version registration for specified simulation");
				m_availableVersions.Add(new SimulationVersion {ExePath = relativeExePath, Version = "Latest"});
			}
			else
			{

				DirectoryInfo baseDirectoryInfo = new DirectoryInfo(relativeExePath.Substring(0, basePathLength));
				foreach (DirectoryInfo directory in baseDirectoryInfo.EnumerateDirectories())
				{
					Regex re = new Regex("^[vV]([0-9.]+)");
					if (re.IsMatch(directory.Name))
					{
						ConsoleLogger.Info("Registered simulation \"" + SimulationType + "\" version \"" +
						                  directory.Name + "\"");
						string executable = relativeExePath.Substring(basePathLength + versionFolderPattern.Length + 1);
						m_availableVersions.Add(new SimulationVersion
							{ExePath = Path.Combine(directory.FullName, executable), Version = directory.Name});
					}
					else
					{
						ConsoleLogger.Warning("Found directory with name \"" + directory.Name +
						                  "\" which does not adhere to the simulation version format ( \"v[0-9.]+\" ) in folder \"" +
						                  baseDirectoryInfo.FullName + "\". Skipping");
					}
				}
			}

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
			SimulationVersion version = m_availableVersions.Find(obj => obj.Version == a_requestedVersion);
			if (version == null)
			{
				ConsoleLogger.Warning("Requested simulation version \"" + a_requestedVersion + "\" for simulation type \"" + SimulationType + "\" which is not known. Falling back to latest version available");
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
			if (string.IsNullOrEmpty(a_simulationRequestSimulationVersion) || a_simulationRequestSimulationVersion == "Latest")
			{
				return true;
			}
			return m_availableVersions.Find(a_obj => a_obj.Version == a_simulationRequestSimulationVersion) != null;
		}
	}
}
