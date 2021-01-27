using System.Collections.Generic;
using System.Net;

namespace MSW
{
	public abstract class RestEndpoint
	{
		private readonly string m_endpointIdentifier;
		public string EndpointIdentifier => m_endpointIdentifier;

		public RestEndpoint(string a_endpointIdentifier)
		{
			m_endpointIdentifier = a_endpointIdentifier;
		}

		public bool ShouldHandleRequest(string a_apiEndpointUri)
		{
			return a_apiEndpointUri.StartsWith(m_endpointIdentifier);
		}

		public abstract void HandleRequest(Dictionary<string, string> a_postValues, HttpListenerResponse a_response);
	}
}