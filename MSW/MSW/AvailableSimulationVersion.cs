using System.Text;

namespace MSW
{
	public class AvailableSimulationVersion
	{
		private readonly AvailableSimulation m_targetSimulation;
		private readonly string m_targetVersion;
		private readonly string m_targetExecutableFullPath;
		public string TargetExecutableFullPath => m_targetExecutableFullPath;
		public string SimulationType => m_targetSimulation.SimulationType;

		public AvailableSimulationVersion(AvailableSimulation a_targetSimulation, string a_targetVersion)
		{
			m_targetSimulation = a_targetSimulation;
			m_targetVersion = a_targetVersion;
			m_targetExecutableFullPath = a_targetSimulation.GetExecutablePathForVersion(a_targetVersion);
		}

		public string GetSimulationTypeAndVersion()
		{
			StringBuilder sb = new StringBuilder(48);
			sb.Append(SimulationType);
			sb.Append(" (").Append(m_targetVersion).Append(") ");
			return sb.ToString();
		}
	}
}
