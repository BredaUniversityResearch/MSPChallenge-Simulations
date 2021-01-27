using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using EwEShell;
using MSWSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MEL
{
	/// <summary>
	/// API connector to connect to a server.
	/// Will connect to the server passed in the constructor, and route to all the data to proper endpoints.
	/// Responses are automatically deserialised in the correct data types for use within MEL
	/// </summary>
	public class ApiMspServer: IApiConnector
	{
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private class APIGetRasterResponse
		{
			//public string displayed_bounds;
			public string image_data = null;
		};


		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private class APIInitialFishingValue
		{
			public string fleet_name;
			public double fishing_value;

			public APIInitialFishingValue(string fleetName, double fishingValue)
			{
				fleet_name = fleetName;
				fishing_value = fishingValue;
			}
		};

		private readonly string m_ServerUrl;
		public bool ShouldRasterizeLayers => true;
		private string m_CurrentAccessToken = "";

		public ApiMspServer(string mspServerAddress)
		{
			m_ServerUrl = mspServerAddress;
		}

		public bool CheckAPIAccess()
		{
			if (HttpGet("/api/security/checkaccess", out APIAccessResult result))
			{
				return result.status != APIAccessResult.EResult.Expired;
			}

			return false;
		}

		public int GetCurrentGameMonth(int lastSimulatedMonth)
		{
			NameValueCollection postValues = new NameValueCollection(1);
			postValues.Add("mel_month", lastSimulatedMonth.ToString());
			if (HttpGet("/api/mel/ShouldUpdate", postValues, out int currentMonth))
			{
				return currentMonth;
			}

			return -100;
		}

		public void SetInitialFishingValues(List<cScalar> fishingValues)
		{
			NameValueCollection values = new NameValueCollection();

			List<APIInitialFishingValue> targetValues = new List<APIInitialFishingValue>(fishingValues.Count);

			foreach (cScalar fish in fishingValues)
			{
				targetValues.Add(new APIInitialFishingValue(fish.Name, fish.Value));
			}

			values.Add("fishing_values", JsonConvert.SerializeObject(targetValues));

			HttpSet("/api/mel/InitialFishing", values);
		}

		public string GetMelConfigAsString()
		{
			if (HttpGet("/api/mel/config", out JToken result))
			{
				return result.ToString();
			}

			return null;
		}

		public string[] GetUpdatedLayers()
		{
			if (HttpGet("/api/mel/Update", out string[] result))
			{
				return result;
			}
			return new string[0];
		}

		public Fishing[] GetFishingValuesForMonth(int month)
		{
			if (month == 0)
			{
				//When the setup ends we are still in lastupdatedmonth 0. We need to get the fishing values for month -1 then to account for starting plans.
				//I am so sorry for this.
				month = -1;
			}

			NameValueCollection postValues = new NameValueCollection(1);
			postValues.Add("game_month", month.ToString());
			if (HttpGet("/api/mel/GetFishing/", postValues, out Fishing[] result))
			{
				return result;
			}

			return new Fishing[0];
		}

		public void SubmitKpi(string kpiName, double kpiValue, string kpiUnits)
		{
			NameValueCollection values = new NameValueCollection
			{
				{ "name" , kpiName },
				{ "value" , kpiValue.ToString() },
				{ "type" , "ECOLOGY" },
				{ "unit" , kpiUnits }
			};
			HttpSet("/api/kpi/post", values);
		}

		public void NotifyTickDone()
		{
			HttpSet("/api/mel/TickDone");
		}

		public double[,] GetRasterLayerByName(string layerName)
		{
			double[,] result = null;
			NameValueCollection postData = new NameValueCollection(1);
			postData.Set("layer_name", layerName);
			if (HttpGet("/api/layer/GetRaster", postData, out APIGetRasterResponse apiResponse))
			{
				byte[] imageBytes = Convert.FromBase64String(apiResponse.image_data);
				using (Stream stream = new MemoryStream(imageBytes))
				{
					using (Bitmap bitmap = new Bitmap(stream))
					{
						result = Rasterizer.PNGToArray(bitmap, 1.0f, MEL.x_res, MEL.y_res);
					}
				}
			}

			return result;
		}

		public void SubmitRasterLayerData(string layerName, Bitmap rasterImage)
		{
			using (MemoryStream stream = new MemoryStream(16384))
			{
				rasterImage.Save(stream, ImageFormat.Png);

				NameValueCollection postData = new NameValueCollection(2);
				postData.Set("layer_name", MEL.ConvertLayerName(layerName));
				postData.Set("image_data", Convert.ToBase64String(stream.ToArray()));
				HttpSet("/api/layer/UpdateRaster", postData);
			}
		}

		public APILayerGeometryData GetLayerData(string layerName, int layerType, bool constructionOnly)
		{
			NameValueCollection values = new NameValueCollection() {
				{"name", layerName },
				{"layer_type", layerType.ToString() },
				{"construction_only", constructionOnly ? "1" : "0" }
			};

			if (HttpGet("/api/mel/GeometryExportName", values, out APILayerGeometryData result))
			{
				return result;
			}

			return null;
		}

		public double[,] GetRasterizedPressure(string name)
		{
			throw new NotImplementedException();
		}

		private bool HttpSet(string apiUrl, NameValueCollection postValues = null)
		{
			return APIRequest.Perform(m_ServerUrl, apiUrl, m_CurrentAccessToken, postValues);
		}

		private bool HttpGet<TTargetType>(string apiUrl, out TTargetType result)
		{
			return HttpGet(apiUrl, null, out result);
		}

		private bool HttpGet<TTargetType>(string apiUrl, NameValueCollection postValues, out TTargetType result)
		{
			return APIRequest.Perform(m_ServerUrl, apiUrl, m_CurrentAccessToken, postValues, out result);
		}

		public void UpdateAccessToken(string newAccessToken)
		{
			m_CurrentAccessToken = newAccessToken;
		}
	}
}