using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MSW
{
	public class RestEndpointSetMonth : RestEndpoint
	{
		public struct RequestData
		{
			public readonly string GameSessionToken;
			public readonly int Month;
			public RequestData( string a_gameSessionToken, int a_month)
			{
				GameSessionToken = a_gameSessionToken;
				Month = a_month;
			}
		}

		private List<RequestData> m_pendingRequests = new List<RequestData>();

		public RestEndpointSetMonth() : base("SetMonth")
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
			bool result = false;
			string requestErrorMessage = "";

			int month = -1;
			if (a_postValues.TryGetValue("game_session_token", out string gameSessionToken) &&
				a_postValues.TryGetValue("month", out string monthStr) && int.TryParse(monthStr, out month))
			{
				RequestData data = new RequestData(gameSessionToken, month);
				lock (m_pendingRequests)
				{
					m_pendingRequests.Add(data);
					result = true;
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
	}
}