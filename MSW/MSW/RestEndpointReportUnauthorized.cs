using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using MSWSupport;

namespace MSW
{
	/// <summary>
	/// REST endpoint that simulation processes call when they receive a 401 Unauthorized
	/// response from the game server API. MSW reacts by triggering an immediate token renewal.
	/// Endpoint: POST /Watchdog/ReportUnauthorized
	/// Required field: game_session_api (the game server API root URL)
	/// </summary>
	public class RestEndpointReportUnauthorized : RestEndpoint
	{
		private readonly Action<string> m_onUnauthorizedReported;

		public RestEndpointReportUnauthorized(Action<string> onUnauthorizedReported)
			: base("ReportUnauthorized")
		{
			m_onUnauthorizedReported = onUnauthorizedReported;
		}

		public override void HandleRequest(Dictionary<string, string> a_postValues, HttpListenerResponse a_response)
		{
			bool success = false;
			string message = "Missing required field: game_session_api";

			if (a_postValues.TryGetValue("game_session_api", out string gameSessionApi) &&
			    !string.IsNullOrWhiteSpace(gameSessionApi))
			{
				ConsoleLogger.Warning($"Simulation reported 401 Unauthorized for server {gameSessionApi}, triggering immediate token renewal");
				m_onUnauthorizedReported(gameSessionApi);
				success = true;
				message = string.Empty;
			}

			string responseString = "{\"success\":" + (success ? "1" : "0") + ",\"message\":" +
			                        Newtonsoft.Json.JsonConvert.ToString(message) + "}";

			byte[] buffer = Encoding.UTF8.GetBytes(responseString);
			a_response.ContentLength64 = buffer.Length;
			Stream output = a_response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();
		}
	}
}

