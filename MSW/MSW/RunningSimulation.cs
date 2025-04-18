﻿using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using MSWSupport;

namespace MSW
{
	//Representation of a running simulation.
	class RunningSimulation
	{
		private readonly AvailableSimulationVersion m_simulationVersion = null;
		private readonly string m_targetApiEndpoint;
		private Process m_runningProcess = null;
		private string m_pipeName;
		private NamedPipeServerStream m_communicationPipeServer;

		public string SimulationType => m_simulationVersion.SimulationType;

		private ApiAccessToken m_currentApiAccessToken;

		public RunningSimulation(AvailableSimulationVersion a_config, string a_apiEndpoint, ApiAccessToken a_currentApiAccessToken)
		{
			m_pipeName = $"MSW_Pipe_{a_config.SimulationType}_${Guid.NewGuid()}";
			m_communicationPipeServer = new NamedPipeServerStream(m_pipeName, PipeDirection.Out);

			m_simulationVersion = a_config;
			m_targetApiEndpoint = a_apiEndpoint;
			m_currentApiAccessToken = a_currentApiAccessToken;
			StartSimulation();
		}

		private void StartSimulation()
		{
			StringBuilder arguments = new StringBuilder(128);
			arguments.Append("MSWPipe=");
			arguments.Append(m_pipeName);
			arguments.Append(" APIEndpoint=");
			arguments.Append(m_targetApiEndpoint);

			string executable = Path.GetFullPath(m_simulationVersion.TargetExecutableFullPath);
			string workingDirectory = Path.GetDirectoryName(executable);

			ConsoleLogger.Info($"Arguments: {arguments.ToString()}");

			ProcessStartInfo startInfo = new ProcessStartInfo(executable, arguments.ToString())
			{
				WorkingDirectory = workingDirectory,
				UseShellExecute = true
			};

			// Pass all environment variables to the child process
			foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
			{
			    startInfo.EnvironmentVariables[(string)envVar.Key] = (string)envVar.Value;
			}

			m_runningProcess = Process.Start(startInfo);

			m_communicationPipeServer.WaitForConnectionAsync().ContinueWith((a_task) => { OnPipeConnected(); });
		}

		public void EnsureSimulationRunning()
		{
			if (m_runningProcess == null || m_runningProcess.HasExited)
			{
				ConsoleLogger.Info($"Simulation {m_simulationVersion.GetSimulationTypeAndVersion()} should be running but is not. Restarting...");
				StartSimulation();
			}
		}

		public void StopSimulation()
		{
			if (!m_runningProcess.HasExited)
			{
				m_runningProcess.Kill();
			}
		}

		public void SetApiAccessToken(ApiAccessToken a_tokenValue)
		{
			m_currentApiAccessToken = a_tokenValue;
			CommunicateUpdatedApiAccessToken();
		}

		public void SetMonth(int a_month)
		{
			if (m_communicationPipeServer.IsConnected)
			{
				try
				{
					using (StreamWriter writer = new StreamWriter(m_communicationPipeServer, Encoding.UTF8, 128, true))
					{
						writer.WriteLine(CommunicationPipeHandler.MONTH_PRELUDE+a_month);
						writer.Flush();
					}
				}
				catch (IOException)
				{
					Console.WriteLine($"Communication pipe {m_pipeName} reported IO Exception. Did the other application exit?");
					m_communicationPipeServer.Disconnect();
					m_communicationPipeServer.WaitForConnectionAsync().ContinueWith((a_task) => { OnPipeConnected(); });
				}
			}
		}

		private void CommunicateUpdatedApiAccessToken()
		{
			if (m_communicationPipeServer.IsConnected)
			{
				try
				{
					using (StreamWriter writer = new StreamWriter(m_communicationPipeServer, Encoding.UTF8, 128, true))
					{
						writer.WriteLine(CommunicationPipeHandler.TOKEN_PRELUDE+m_currentApiAccessToken.GetTokenAsString());
						writer.Flush();
					}
				}
				catch (IOException)
				{
					ConsoleLogger.Error($"Communication pipe {m_pipeName} reported IO Exception. Did the other application exit?");
					m_communicationPipeServer.Disconnect();
					m_communicationPipeServer.WaitForConnectionAsync().ContinueWith((a_task) => { OnPipeConnected(); });
				}
			}
		}

		private void OnPipeConnected()
		{
			ConsoleLogger.Info("Pipe connected " + m_pipeName);
			CommunicateUpdatedApiAccessToken();
		}

		public string GetCommunicationPipeName()
		{
			return m_pipeName;
		}

		public void PingCommunicationPipe()
		{
			try
			{
				if (m_communicationPipeServer.IsConnected)
				{
					using (StreamWriter writer = new StreamWriter(m_communicationPipeServer, Encoding.UTF8, 128, true))
					{
						writer.WriteLine("KeepAliveMessage");
					}
				}
			}
			catch (IOException)
			{
				ConsoleLogger.Error($"Communication pipe {m_pipeName} reported IO Exception. Did the other application exit?");
				m_communicationPipeServer.Disconnect();
				m_communicationPipeServer.WaitForConnectionAsync().ContinueWith((a_task) => { OnPipeConnected(); });
			}
		}
	}
}
