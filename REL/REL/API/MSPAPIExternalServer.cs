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

		public void UpdateAccessToken(string newAccessToken)
		{
			m_currentAccessToken = newAccessToken;
		}

		private string HttpGet(string a_apiEndPoint, NameValueCollection a_postValues = null)
		{
			WebClient webclient = new WebClient();
			webclient.Headers.Add(MSWConstants.APITokenHeader, m_currentAccessToken);
			if (a_postValues == null) a_postValues = new NameValueCollection();
			byte[] response = webclient.UploadValues(RELConfig.Instance.GetAPIRoot() + a_apiEndPoint, a_postValues);

			return Encoding.UTF8.GetString(response);
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

		private static TResultType DeserializeJson<TResultType>(string a_jsonData)
		{
			try
			{
				return JsonConvert.DeserializeObject<TResultType>(a_jsonData, APIUtils.AvailableConverters);
			}
			catch (JsonSerializationException ex)
			{
				Console.WriteLine("Error deserializing JSON String.");
				Console.WriteLine("Exception: " + ex.Message);
				Console.WriteLine("InputData: ");
				Console.WriteLine(a_jsonData);
				return default;
			}
			catch (JsonReaderException ex)
			{
				Console.WriteLine("Error deserializing JSON String.");
				Console.WriteLine("Exception: " + ex.Message);
				Console.WriteLine("InputData: ");
				Console.WriteLine(a_jsonData);
				return default;
			}
		}

		private TResultType RequestAndDeserialize<TResultType>(string a_apiEndpoint, NameValueCollection a_postValues = null)
		{
			string data = HttpGet(a_apiEndpoint, a_postValues);
			return DeserializeJson<TResultType>(data);
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
			HttpSet("/api/layer/UpdateRaster", postData);
		}
	}
}
