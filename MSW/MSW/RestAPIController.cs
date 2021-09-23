using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace MSW
{
	class RestApiController: IDisposable
	{
		private const string PrefixHost = "http://+:45000/";
		private const string ApiUriIdentifier = "Watchdog/";

		private HttpListener m_updateGameStateListener = null;
		private Thread m_backgroundProcessThread = null;

		private List<RestEndpoint> m_endpoints = new List<RestEndpoint>();

		public RestApiController()
		{
			m_updateGameStateListener = new HttpListener();
			m_updateGameStateListener.Prefixes.Add(PrefixHost + ApiUriIdentifier);
			m_updateGameStateListener.Start();
			Console.WriteLine("Starting REST API at " + PrefixHost + ApiUriIdentifier);

			m_backgroundProcessThread = new Thread(StartHandlingRequestsBackground);
			m_backgroundProcessThread.Start();
		}

		private void StartHandlingRequestsBackground()
		{
			while (m_updateGameStateListener.IsListening)
			{
				HttpListenerContext context = m_updateGameStateListener.GetContext();
				HandleRequestBackground(context);
			}
		}

		private void HandleRequestBackground(HttpListenerContext a_context)
		{
			Console.WriteLine("Handling request at " + a_context.Request.Url);

			RestEndpoint endpoint = FindEndpointForUri(a_context.Request.Url);

			if (endpoint != null)
			{
				Dictionary<string, string> postValues = GetPostValuesFromRequest(a_context);
				endpoint.HandleRequest(postValues, a_context.Response);
			}
			else
			{
				a_context.Response.StatusCode = 404;
			}
		}

		private RestEndpoint FindEndpointForUri(Uri a_requestUrl)
		{
			RestEndpoint result = null;
			int identifier = a_requestUrl.AbsoluteUri.IndexOf(ApiUriIdentifier, StringComparison.InvariantCultureIgnoreCase);
			if (identifier > 0)
			{
				string endpoint = a_requestUrl.AbsoluteUri.Substring(identifier + ApiUriIdentifier.Length);
				foreach (RestEndpoint availableEndpoint in m_endpoints)
				{
					if (availableEndpoint.ShouldHandleRequest(endpoint))
					{
						result = availableEndpoint;
						break;
					}
				}
			}

			return result;
		}

		private Dictionary<string, string> GetPostValuesFromRequest(HttpListenerContext a_context)
		{
			Dictionary<string, string> result = new Dictionary<string, string>();
			if (a_context.Request.HasEntityBody)
			{
				StreamReader reader =
					new StreamReader(a_context.Request.InputStream, a_context.Request.ContentEncoding);

				string bodyText = reader.ReadToEnd();

				string[] postParams = bodyText.Split('&');
				foreach (string param in postParams)
				{
					string[] keyValuePair = param.Split('=');
					string value = WebUtility.UrlDecode(keyValuePair[1]);
					result.Add(keyValuePair[0], value);
				}
			}

			return result;
		}

		public void StopHandlingRequests()
		{
			m_updateGameStateListener.Stop();
			m_backgroundProcessThread.Join();
			m_backgroundProcessThread = null;
		}

		public void Dispose()
		{
			((IDisposable)m_updateGameStateListener)?.Dispose();
		}

		public void AddEndpoint(RestEndpoint a_endpoint)
		{
			Console.WriteLine("Registered REST Endpoint " + a_endpoint.EndpointIdentifier);
			m_endpoints.Add(a_endpoint);
		}
	}
}
