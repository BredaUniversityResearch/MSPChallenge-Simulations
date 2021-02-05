using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MSW
{
	public enum EGameState
	{
		Setup,
		Play,
		Simulation,
		Pause,
		End
	}

	public class RestEndpointUpdateState : RestEndpoint
	{
		public struct RequestData
		{
			public struct SimulationRequest
			{
				public string SimulationType;
				public string SimulationVersion;
			};

			public readonly string GameSessionApi;
			public readonly string GameSessionToken;
			public readonly EGameState GameState;
			public readonly SimulationRequest[] ConfiguredSimulations;
			public readonly ApiAccessToken AccessToken;
			public readonly ApiAccessToken RecoveryToken;

			public RequestData(string a_gameSessionApi, string a_gameSessionToken, EGameState a_gameState, SimulationRequest[] a_configuredSimulations, ApiAccessToken a_accessToken, ApiAccessToken a_recoveryToken)
			{
				GameSessionApi = a_gameSessionApi;
				GameSessionToken = a_gameSessionToken;
				GameState = a_gameState;
				ConfiguredSimulations = a_configuredSimulations;
				AccessToken = a_accessToken;
				RecoveryToken = a_recoveryToken;
			}
		}

		private List<RequestData> m_pendingRequests = new List<RequestData>();
		private AvailableSimulation[] m_availableSimulations = null;

		public RestEndpointUpdateState(AvailableSimulation[] a_availableSimulations) 
			: base("UpdateState")
		{
			m_availableSimulations = a_availableSimulations;
		}

		public RequestData[] GetPendingRequestData()
		{
			lock (m_pendingRequests)
			{
				RequestData[] requests = m_pendingRequests.ToArray();
				m_pendingRequests.Clear();
				return requests;
			}
		}

		public override void HandleRequest(Dictionary<string, string> a_postValues, HttpListenerResponse a_response)
		{
			bool result = false;
			string requestErrorMessage = "";

			if (a_postValues.TryGetValue("game_session_api", out string gameSession) && 
				a_postValues.TryGetValue("game_session_token", out string gameSessionToken) &&
				a_postValues.TryGetValue("game_state", out string gameState) &&
				a_postValues.TryGetValue("required_simulations", out string requiredSimulations) &&
				a_postValues.TryGetValue("api_access_token", out string apiAccessToken) &&
				a_postValues.TryGetValue("api_access_renew_token", out string apiAccessRenewToken))
			{
				RequestData.SimulationRequest[] requestedSimulations;
				if (!string.IsNullOrEmpty(requiredSimulations))
				{
					string decoded = WebUtility.UrlDecode(requiredSimulations);
					JArray simulationRequests = JArray.Parse(decoded);
					requestedSimulations = new RequestData.SimulationRequest[simulationRequests.Count];

					int requestIndex = 0;
					foreach (var arrayToken in simulationRequests)
					{
						if (arrayToken is JProperty value)
						{
							requestedSimulations[requestIndex].SimulationType = value.Name;
							requestedSimulations[requestIndex].SimulationVersion = value.Value.ToString();
							++requestIndex;
						}
						else
						{
							ConsoleLogger.Warning(
								$"Got value in Simulations Request that is not a property. Full string: {decoded}");
						}
					}
				}
				else
				{
					requestedSimulations = new RequestData.SimulationRequest[0];
				}

				if (Enum.TryParse(gameState, true, out EGameState parsedGameState))
				{
					RequestData data = new RequestData(gameSession, gameSessionToken, parsedGameState, requestedSimulations, 
						JsonConvert.DeserializeObject<ApiAccessToken>(apiAccessToken), JsonConvert.DeserializeObject<ApiAccessToken>(apiAccessRenewToken));
					if (CheckRequest(data, out requestErrorMessage))
					{
						lock (m_pendingRequests)
						{
							m_pendingRequests.Add(data);
							result = true;
						}
					}
					else
					{
						Console.WriteLine("Request did not pass checks. Error message: " + requestErrorMessage);
					}
				}
			}
			else
			{
				requestErrorMessage = "Request incomplete. Missing required fields";
			}

			string responseString = "{\"success\":" + ((result) ? "1" : "0") + ",\"message\": " + JsonConvert.ToString(requestErrorMessage) + "	}";

			Encoding encoding = a_response.ContentEncoding ?? Encoding.UTF8;
			byte[] buffer = encoding.GetBytes(responseString);
			a_response.ContentLength64 = buffer.Length;
			Stream output = a_response.OutputStream;
			output.Write(buffer, 0, buffer.Length);

			output.Close();
		}

		private bool CheckRequest(RequestData a_data, out string a_errorMessage)
		{
			StringBuilder errorOutput = new StringBuilder(128);
			bool result = true;
			foreach (RequestData.SimulationRequest simulationRequest in a_data.ConfiguredSimulations)
			{
				AvailableSimulation availableSim = Array.Find(m_availableSimulations, a_obj => a_obj.SimulationType == simulationRequest.SimulationType);
				if (availableSim == null)
				{
					errorOutput.AppendLine("Unknown requested simulation \""+simulationRequest.SimulationType+"\"");
					result = false;
				}
				else
				{
					if (!availableSim.HasVersionAvailable(simulationRequest.SimulationVersion))
					{
						errorOutput.AppendLine("Requested simulation version \"" + simulationRequest.SimulationVersion + "\" is not available for simulation \"" +
											   simulationRequest.SimulationType + "\".");
						result = false;
					}
				}
			}

			a_errorMessage = errorOutput.ToString();
			return result;
		}
	}
}