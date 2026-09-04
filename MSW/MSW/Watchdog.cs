using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MSWSupport;
using Newtonsoft.Json;

namespace MSW
{
	class Watchdog
	{
		private class ServerData
		{
			/// <summary>Full token renewal interval (proactive keep-alive).</summary>
			private const long TokenCheckIntervalSec = 900;

			/// <summary>
			/// Lightweight token validity probe interval.
			/// If the token is invalid at probe time MSW renews immediately.
			/// </summary>
			private const long TokenHealthCheckIntervalSec = 60;

			public readonly string ServerApiRoot;
			public readonly string ServerWatchdogToken;
			public readonly AvailableSimulationVersion[] ConfiguredSimulations = null;
			public List<RunningSimulation> RunningSimulations = new List<RunningSimulation>();

			private ApiAccessToken m_currentAccessToken;
			private ApiAccessToken m_recoveryToken;
			private Task m_checkTokenTask = null;
			private DateTime m_lastTokenCheckTime;
			private DateTime m_lastHealthCheckTime = DateTime.MinValue;

			/// <summary>Set to true by TriggerImmediateTokenRenewal to request out-of-schedule renewal.</summary>
			private volatile bool m_immediateRenewalRequested = false;

			private readonly int m_mswPort;

			public EGameState CurrentState
			{
				get;
				private set;
			}

			public DateTime LastStateChangeTime
			{
				get;
				private set;
			}

			public ServerData(string a_serverApiRoot, string a_serverWatchdogToken, AvailableSimulationVersion[] a_configuredSimulations, ApiAccessToken a_currentAccessToken, ApiAccessToken a_recoveryToken, int a_mswPort)
			{
				ServerApiRoot = a_serverApiRoot;
				ServerWatchdogToken = a_serverWatchdogToken;
				ConfiguredSimulations = a_configuredSimulations;
				m_currentAccessToken = a_currentAccessToken;
				m_recoveryToken = a_recoveryToken;
				m_mswPort = a_mswPort;

				m_lastTokenCheckTime = DateTime.Now;
			}

			private RunningSimulation FindRunningSimulationOfType(AvailableSimulationVersion a_simulationType)
			{
				return RunningSimulations.Find(a_obj => a_obj.SimulationType == a_simulationType.SimulationType);
			}

			private void StartSimulationOfType(AvailableSimulationVersion a_simulationVersion)
			{
				RunningSimulation simulation = new RunningSimulation(a_simulationVersion, ServerApiRoot, m_currentAccessToken, m_mswPort);
				RunningSimulations.Add(simulation);
			}

			public void EnsureSimulationsRunning()
			{
				AvailableSimulationVersion[] configuredSimulations = ConfiguredSimulations;
				foreach (AvailableSimulationVersion simulationType in configuredSimulations)
				{
					RunningSimulation simulation = FindRunningSimulationOfType(simulationType);
					if (simulation == null)
					{
						ConsoleLogger.Info($"Starting simulation {simulationType.GetSimulationTypeAndVersion()} for {ServerApiRoot} on session token {ServerWatchdogToken}");

						StartSimulationOfType(simulationType);
					}
					else
					{
						simulation.EnsureSimulationRunning();
					}
				}
			}

			public void StopAllSimulations()
			{
				foreach (RunningSimulation simulation in RunningSimulations)
				{
					ConsoleLogger.Info($"Killing simulation {simulation.SimulationType} for {ServerApiRoot}");
					simulation.StopSimulation();
				}

				RunningSimulations.Clear();
			}

			public void SetCurrentState(EGameState a_state)
			{
				CurrentState = a_state;
				LastStateChangeTime = DateTime.Now;
			}

			public void SetApiAccessToken(string token)
			{
				m_currentAccessToken.SetToken(token);
				foreach (RunningSimulation sim in RunningSimulations)
				{
					sim.SetApiAccessToken(m_currentAccessToken);
				}
			}

			public void SetMonth(int month)
			{
				foreach (RunningSimulation sim in RunningSimulations)
				{
					sim.SetMonth(month);
				}
			}

			public void SetApiRecoveryToken(string token)
			{
				m_recoveryToken.SetToken(token);
			}

			/// <summary>
			/// Signals MSW to renew the token on the next UpdateAccessTokens tick instead of waiting
			/// for the regular 900-second interval. Called when a simulation reports a 401.
			/// </summary>
			public void TriggerImmediateTokenRenewal(string reason)
			{
				ConsoleLogger.Warning($"Immediate token renewal requested for {ServerApiRoot}: {reason}");
				m_immediateRenewalRequested = true;
			}

			public void UpdateAccessTokens()
			{
				// Skip if a task is already in progress
				if (m_checkTokenTask != null && !m_checkTokenTask.IsCompleted)
					return;

				// Highest priority: immediate renewal requested by a simulation reporting 401
				if (m_immediateRenewalRequested)
				{
					m_immediateRenewalRequested = false;
					ConsoleLogger.Info($"Executing immediate token renewal for {ServerApiRoot}");
					m_lastTokenCheckTime = DateTime.Now; // reset regular timer
					m_lastHealthCheckTime = DateTime.Now; // reset health-check timer
					m_checkTokenTask = Task.Run(CheckTokenTask);
					return;
				}

				// Scheduled 900-second full renewal
				if ((DateTime.Now - m_lastTokenCheckTime).TotalSeconds > TokenCheckIntervalSec)
				{
					m_lastTokenCheckTime = DateTime.Now;
					m_checkTokenTask = Task.Run(CheckTokenTask);
					return;
				}

				// Proactive 60-second health probe – renews only if the token is actually invalid
				if ((DateTime.Now - m_lastHealthCheckTime).TotalSeconds > TokenHealthCheckIntervalSec)
				{
					m_lastHealthCheckTime = DateTime.Now;
					m_checkTokenTask = Task.Run(HealthCheckTask);
				}
			}

			private void CheckTokenTask()
			{
				foreach (RunningSimulation simulation in RunningSimulations)
				{
					simulation.PingCommunicationPipe();
				}
				RenewToken();
			}

			/// <summary>
			/// Calls a lightweight API endpoint to verify the current token.
			/// If a 401 is received the token has expired and is renewed immediately.
			/// Other errors (network, server down) are logged but do not trigger renewal.
			/// </summary>
			private void HealthCheckTask()
			{
				foreach (RunningSimulation simulation in RunningSimulations)
				{
					simulation.PingCommunicationPipe();
				}

				bool tokenValid = true;
				try
				{
					bool isOnline = APIRequest.Perform(
						ServerApiRoot,
						"api/game/IsOnline",
						out string _,
						m_currentAccessToken.GetTokenAsString(),
						new NameValueCollection()
					);
					if (isOnline)
					{
						ConsoleLogger.Info($"Token health check OK for {ServerApiRoot}");
					}
					else
					{
						ConsoleLogger.Warning($"Token health check: API not online for {ServerApiRoot}");
					}
				}
				catch (ApiUnauthorizedWebException)
				{
					ConsoleLogger.Warning($"Token health check received 401 Unauthorized for {ServerApiRoot} – renewing token immediately");
					tokenValid = false;
				}
				catch (Exception ex)
				{
					// Network error or server down – don't renew, wait for next probe
					ConsoleLogger.Warning($"Token health check failed for {ServerApiRoot}: {ex.Message}");
				}

				if (!tokenValid)
				{
					RenewToken();
				}
				else
				{
					m_checkTokenTask = null;
				}
			}

			private void RenewToken()
			{
				ConsoleLogger.Info($"Requesting new access token from server {ServerApiRoot}");

				string tokenToUse = m_currentAccessToken.GetTokenAsString(); //although not necessary for this endpoint
				NameValueCollection postValues = new NameValueCollection(1);
				postValues.Add("api_refresh_token", m_recoveryToken.GetTokenAsString());

				bool callSuccess = APIRequest.Perform(
					ServerApiRoot,
					"api/User/RequestToken",
					out ApiUserRequestTokenResponse response,
					tokenToUse,
					postValues
				);
				if (callSuccess == false)
				{
					ConsoleLogger.Info("Request to get new token failed...");
					ConsoleLogger.Info("Bearer token: " + tokenToUse);
					ConsoleLogger.Info("Posted api_refresh_token: " + m_recoveryToken.GetTokenAsString());
				}
				else
				{
					SetApiAccessToken(response.api_access_token);
					ConsoleLogger.Info($"Successfully updated access token for server {ServerApiRoot}");
					SetApiRecoveryToken(response.api_refresh_token);
					ConsoleLogger.Info($"Successfully updated refresh token for server {ServerApiRoot}");
				}

				m_checkTokenTask = null;
			}
		}

		private static readonly TimeSpan InactiveSimulationTime = new TimeSpan(72, 0, 0); //Kill simulations after a period of time. Please keep this quite royal since currently restarting MEL is not 100% accurate.

		private List<ServerData> m_activeServers = new List<ServerData>(8);
		private RestApiController m_restApiController;
		private RestEndpointUpdateState m_updateStateEndpoint;
		private RestEndpointSetMonth m_setMonthEndpoint;
		private RestEndpointReportUnauthorized m_reportUnauthorizedEndpoint;
		private RestEndpointReportSessionGone m_reportSessionGoneEndpoint;
		private List<AvailableSimulation> m_availableSimulations = new List<AvailableSimulation>(8);
		private readonly int m_restApiPort;

		private MSWPipeDebugConnector m_debugConnector = null;

		public Watchdog(int a_restApiPort = RestApiController.DEFAULT_PORT)
		{
			m_restApiPort = a_restApiPort;
			m_restApiController = new RestApiController(a_restApiPort);
			m_debugConnector = new MSWPipeDebugConnector(FindServerSimulationPipeNameByWatchdogToken);
			foreach (SimulationConfig config in MswConfig.Instance.GetAllSimulationConfig())
			{
				m_availableSimulations.Add(new AvailableSimulation(config));
			}

			m_updateStateEndpoint = new RestEndpointUpdateState(m_availableSimulations.ToArray());
			m_setMonthEndpoint = new RestEndpointSetMonth();
			m_reportUnauthorizedEndpoint = new RestEndpointReportUnauthorized(HandleReportUnauthorized);
			m_reportSessionGoneEndpoint = new RestEndpointReportSessionGone();
			m_restApiController.AddEndpoint(m_updateStateEndpoint);
			m_restApiController.AddEndpoint(m_setMonthEndpoint);
			m_restApiController.AddEndpoint(m_reportUnauthorizedEndpoint);
			m_restApiController.AddEndpoint(m_reportSessionGoneEndpoint);
		}

		public void Tick()
		{
			// set month
			{
				RestEndpointSetMonth.RequestData[] requests = m_setMonthEndpoint.GetPendingRequestData();
				foreach (RestEndpointSetMonth.RequestData request in requests)
				{
					HandleSetMonthRequest(request);
				}
			}
			// update state
			{
				RestEndpointUpdateState.RequestData[] requests = m_updateStateEndpoint.GetPendingRequestData();
				foreach (RestEndpointUpdateState.RequestData request in requests)
				{
					HandleUpdateStateRequest(request);
				}
			}
			// report session gone
			{
				RestEndpointReportSessionGone.RequestData[] requests = m_reportSessionGoneEndpoint.GetPendingRequestData();
				foreach (RestEndpointReportSessionGone.RequestData request in requests)
				{
					HandleReportSessionGoneRequest(request);
				}
			}

			UpdateAccessTokensForRunningSimulations();

			StopSimulationsForInactiveSessions();
		}

		private void UpdateAccessTokensForRunningSimulations()
		{
			foreach (ServerData data in m_activeServers)
			{
				data.UpdateAccessTokens();
			}
		}

		private void StopSimulationsForInactiveSessions()
		{
			for (int i = m_activeServers.Count - 1; i >= 0; --i)
			{
				ServerData data = m_activeServers[i];
				if (IsIdleState(data.CurrentState))
				{
					if (DateTime.Now - data.LastStateChangeTime > InactiveSimulationTime)
					{
						data.StopAllSimulations();
						m_activeServers.RemoveAt(i);
					}
				}
				else if (IsStoppedState(data.CurrentState))
				{
					data.StopAllSimulations();
					m_activeServers.RemoveAt(i);
				}
			}
		}

		private bool IsIdleState(EGameState a_state)
		{
			return a_state == EGameState.Pause || a_state == EGameState.Setup;
		}

		private bool IsStoppedState(EGameState a_state)
		{
			return a_state == EGameState.End;
		}

		private void HandleSetMonthRequest(RestEndpointSetMonth.RequestData a_request)
		{
			ServerData? existingData = FindServerDataForSessionToken(a_request.GameSessionToken);
			if (existingData == null)
			{
				return;
			}
			ConsoleLogger.Info("Setting month to " + a_request.Month);
			existingData.SetMonth(a_request.Month);
		}

		private void HandleUpdateStateRequest(RestEndpointUpdateState.RequestData a_request)
		{
			ServerData? existingData = FindServerDataForSessionToken(a_request.GameSessionToken);
			if (existingData != null)
			{
				if (!IsStoppedState(a_request.GameState))
				{
					existingData.EnsureSimulationsRunning();
				}
				else
				{
					existingData.StopAllSimulations();
					m_activeServers.Remove(existingData);
					ConsoleLogger.Info($"Stopped simulation server instance for {a_request.GameSessionApi}");
				}
				existingData.SetCurrentState(a_request.GameState);
				existingData.SetApiAccessToken(a_request.AccessToken.GetTokenAsString());
				existingData.SetApiRecoveryToken(a_request.RecoveryToken.GetTokenAsString());
				existingData.SetMonth(a_request.Month);
			}
			else
			{
				if (!IsStoppedState(a_request.GameState))
				{
					AvailableSimulationVersion[] targetSimulationVersions = new AvailableSimulationVersion[a_request.ConfiguredSimulations.Length];
					for (int i = 0; i < a_request.ConfiguredSimulations.Length; ++i)
					{
						RestEndpointUpdateState.RequestData.SimulationRequest simulationType = a_request.ConfiguredSimulations[i];
						AvailableSimulation sim = m_availableSimulations.Find(obj => obj.SimulationType == simulationType.SimulationType);
						if (sim != null)
						{
							targetSimulationVersions[i] = sim.GetSimulationVersion(simulationType.SimulationVersion);
						}
						else
						{
							throw new Exception("Unknown simulation with type " + simulationType.SimulationType);
						}
					}

					ServerData data = new ServerData(a_request.GameSessionApi, a_request.GameSessionToken, targetSimulationVersions, a_request.AccessToken, a_request.RecoveryToken, m_restApiPort);
					m_activeServers.Add(data);
					data.EnsureSimulationsRunning();
					data.SetCurrentState(a_request.GameState);
					data.SetApiAccessToken(a_request.AccessToken.GetTokenAsString());
					data.SetApiRecoveryToken(a_request.RecoveryToken.GetTokenAsString());
					data.SetMonth(a_request.Month);
					ConsoleLogger.Info("Created new simulation server instance for " + a_request.GameSessionApi);
				}
			}
		}

		private ServerData? FindServerDataForSessionToken(string a_sessionToken)
		{
			return m_activeServers.Find(a_obj => a_obj.ServerWatchdogToken == a_sessionToken);
		}

		/// <summary>
		/// Called by RestEndpointReportUnauthorized when a simulation has received a 401 from
		/// the game server. Triggers immediate token renewal for the matching server session.
		/// </summary>
		private void HandleReportUnauthorized(string gameSessionApi)
		{
			ServerData data = m_activeServers.Find(s =>
				string.Equals(s.ServerApiRoot, gameSessionApi, StringComparison.OrdinalIgnoreCase) ||
				gameSessionApi.StartsWith(s.ServerApiRoot, StringComparison.OrdinalIgnoreCase));

			if (data != null)
			{
				data.TriggerImmediateTokenRenewal($"simulation reported 401 for {gameSessionApi}");
			}
			else
			{
				ConsoleLogger.Warning($"ReportUnauthorized: no active server session found matching {gameSessionApi}");
			}
		}

		private void HandleReportSessionGoneRequest(RestEndpointReportSessionGone.RequestData request)
		{
			ServerData data = m_activeServers.Find(s =>
				string.Equals(s.ServerApiRoot, request.GameSessionApi, StringComparison.OrdinalIgnoreCase) ||
				request.GameSessionApi.StartsWith(s.ServerApiRoot, StringComparison.OrdinalIgnoreCase));

			if (data != null)
			{
				ConsoleLogger.Warning($"Simulation reported HTTP 410 Gone for {request.GameSessionApi}. Stopping simulations and removing this session from watchdog state.");
				data.StopAllSimulations();
				m_activeServers.Remove(data);
			}
			else
			{
				ConsoleLogger.Warning($"ReportSessionGone: no active server session found matching {request.GameSessionApi}");
			}
		}

		private string FindServerSimulationPipeNameByWatchdogToken(string a_watchdogToken, string a_simulationType)
		{
			string result = null;
			ServerData data = m_activeServers.Find(obj => obj.ServerWatchdogToken == a_watchdogToken);
			if (data != null)
			{
				RunningSimulation configuredSimulation = data.RunningSimulations.Find((a_runningSimulation) => a_runningSimulation.SimulationType == a_simulationType);
				if (configuredSimulation != null)
				{
					result = configuredSimulation.GetCommunicationPipeName();
				}
			}

			return result;
		}
	}
}
