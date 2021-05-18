using System;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using MSWSupport;
using Newtonsoft.Json;

namespace REL.API
{
	class MSPAPIExternalServer: IMSPAPIConnector
	{
		private string m_currentAccessToken = null;
		private JsonSerializer m_jsonSerializerSettings = new JsonSerializer();

		public MSPAPIExternalServer()
		{
			m_jsonSerializerSettings.Converters.Add(new JsonConverterVector2D());
		}

		public void UpdateAccessToken(string newAccessToken)
		{
			m_currentAccessToken = newAccessToken;
		}

		private void HttpSet(string apiEndPoint, NameValueCollection postValues)
		{
			WebClient webclient = new WebClient();
			webclient.Headers.Add(MSWConstants.APITokenHeader, m_currentAccessToken);

			try
			{
				byte[] response = webclient.UploadValues(RELConfig.Instance.GetAPIRoot() + apiEndPoint, postValues);
				if (response.Length > 0)
				{
					Console.WriteLine("Unexpected response from a HttpSet call to URL {0}. Response: \n{1}", apiEndPoint, Encoding.UTF8.GetString(response));
				}
			}
			catch (WebException ex)
			{
				Console.WriteLine($"Exception occurred during HttpSet. {ex.Message}");
			}
		}

		private TResultType RequestAndDeserialize<TResultType>(string a_apiEndpoint, NameValueCollection a_postValues = null)
		{
			if (APIRequest.Perform(RELConfig.Instance.GetAPIRoot(), a_apiEndpoint, m_currentAccessToken, a_postValues,
				out TResultType result, m_jsonSerializerSettings))
			{
				return result;
			}

			Console.WriteLine($"API Request to {a_apiEndpoint} failed");
			return default;
		}

		public MSPAPIDate GetDateForSimulatedMonth(int a_simulatedMonth)
		{
			NameValueCollection data = new NameValueCollection {
				{"simulated_month", a_simulatedMonth.ToString()}
			};
			return RequestAndDeserialize<MSPAPIDate>("/api/Game/GetActualDateForSimulatedMonth", data);
		}

		public MSPAPIGeometry[] GetGeometry()
		{
			return RequestAndDeserialize<MSPAPIGeometry[]>("/api/REL/GetRestrictionGeometry");
		}

		public MSPAPIRELConfig GetConfiguration()
		{
			return RequestAndDeserialize<MSPAPIRELConfig>("/api/REL/GetConfiguration");
		}

		public void UpdateRaster(string a_layerName, Bounds2D a_bounds, byte[] a_rasterImageData)
		{
			NameValueCollection postData = new NameValueCollection(3);
			postData.Set("layer_name", a_layerName);
			postData.Set("raster_bounds", JsonConvert.SerializeObject(a_bounds.ToArray()));
			postData.Set("image_data", Convert.ToBase64String(a_rasterImageData));
			if (!APIRequest.Perform(RELConfig.Instance.GetAPIRoot(), "/api/layer/UpdateRaster", m_currentAccessToken,
				postData))
			{
				Console.WriteLine("API Request to UpdateRaster failed");
			}
		}
	}
}
