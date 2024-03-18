using System;
using System.Collections.Specialized;
using MSWSupport;
using Newtonsoft.Json;

namespace REL.API
{
	class MSPAPIExternalServer: ApiConnectorBase, IMSPAPIConnector
	{
		private JsonSerializer m_jsonSerializerSettings = new JsonSerializer();

		public MSPAPIExternalServer(): base(RELConfig.Instance.GetAPIRoot())
		{
			m_jsonSerializerSettings.Converters.Add(new JsonConverterVector2D());
		}

		private TResultType RequestAndDeserialize<TResultType>(string a_apiEndpoint, NameValueCollection a_postValues = null)
		{
			if (HttpSet(a_apiEndpoint, a_postValues, out TResultType result, m_jsonSerializerSettings))
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
			if (!HttpSet("/api/layer/UpdateRaster", postData))
			{
				Console.WriteLine("API Request to UpdateRaster failed");
			}
		}
	}
}
