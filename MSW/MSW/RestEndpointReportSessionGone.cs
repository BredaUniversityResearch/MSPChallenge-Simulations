using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using MSWSupport;

namespace MSW
{
	/// <summary>
	/// Endpoint: POST /Watchdog/ReportSessionGone
	/// Required field: game_session_api (the game server API root URL)
	/// </summary>
	public class RestEndpointReportSessionGone : RestEndpoint
	{
		public struct RequestData
		{
			public readonly string GameSessionApi;

			public RequestData(string gameSessionApi)
			{
				GameSessionApi = gameSessionApi;
			}
		}

		private readonly List<RequestData> m_pendingRequests = new List<RequestData>();

		public RestEndpointReportSessionGone()
			: base(MSWConstants.MSWReportSessionGoneEndpoint)
		{
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
			bool success = false;
			string message = "Missing required field: game_session_api";

			if (a_postValues.TryGetValue("game_session_api", out string gameSessionApi) &&
			    !string.IsNullOrWhiteSpace(gameSessionApi))
			{
				lock (m_pendingRequests)
				{
					m_pendingRequests.Add(new RequestData(gameSessionApi));
				}
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
