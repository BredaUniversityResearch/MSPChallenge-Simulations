using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using MSWSupport;

namespace SEL.API
{
	class APIConnectorExternalServer : IApiConnector
	{
		private class APIMonthContainer
		{
			public int game_currentmonth = 0;
		};

		private string m_accessToken = null;

		public void UpdateAccessToken(string newAccessToken)
		{
			m_accessToken = newAccessToken;
		}

		public bool CheckApiAccess()
		{
			HttpGet("/api/Security/CheckAccess", null, out APIAccessResult result);
			return result != null && result.status != APIAccessResult.EResult.Expired;
		}

		public APISELRegionSettings GetSELRegionSettings()
		{
			HttpGet("/api/SEL/GetSELConfig", null, out APISELRegionSettings result);
			return result;
		}

		public APIHeatmapOutputSettings[] GetHeatmapOutputSettings()
		{
			HttpGet("/api/SEL/GetHeatmapOutputSettings", null, out APIHeatmapOutputSettings[] result);
			return result;
		}

		public APIHeatmapSettings GetHeatmapSettings()
		{
			HttpGet("/api/SEL/GetHeatmapSettings", null, out APIHeatmapSettings result);
			return result;
		}

		public int GetCurrentMonth()
		{
			if (HttpGet("/api/Game/GetCurrentMonth", null, out APIMonthContainer result))
			{
				return result.game_currentmonth;
			}

			return -100;
		}

		public APIUpdateDescriptor GetUpdatePackage()
		{
			HttpGet("/api/SEL/GetUpdatePackage", null, out APIUpdateDescriptor result);
			return result;
		}

		public APIAreaOutputConfiguration GetAreaOutputConfiguration()
		{
			HttpGet("/api/SEL/GetAreaOutputConfiguration", null, out APIAreaOutputConfiguration result);
			return result;
		}

		public APIConfiguredIntensityRoute[] GetConfiguredRouteIntensities()
		{
			HttpGet("/api/SEL/GetConfiguredRouteIntensities", null, out APIConfiguredIntensityRoute[] result);
			return result;
		}

		public APIEEZGeometryData[] GetEEZGeometryData()
		{
			HttpGet("/api/SEL/GetCountryBorderGeometry", null, out APIEEZGeometryData[] result);
			return result;
		}

		public APIShippingRestrictionGeometry[] GetRestrictionGeometry()
		{
			HttpGet("/api/SEL/GetRestrictionGeometry", null, out APIShippingRestrictionGeometry[] result);
			return result;
		}

		public APIShippingLaneGeometry[] GetShippingLanes()
		{
			HttpGet("/api/SEL/GetShippingLaneGeometry", null, out APIShippingLaneGeometry[] result);
			return result;
		}

		public APIShippingPortGeometry[] GetShippingPortGeometry()
		{
			HttpGet("/api/SEL/GetShippingPortGeometry", null, out APIShippingPortGeometry[] result);
			return result;
		}

		public APIShippingPortIntensity[] GetShippingPortIntensity()
		{
			if (Environment.CommandLine.Contains("FakePortIntensity"))
			{
				return DebugGenerateIntensityDataForAllPorts(this);
			}
			else
			{
				HttpGet("/api/SEL/GetPortIntensities", null, out APIShippingPortIntensity[] result);
				return result;
			}
		}

		public APIShipType[] GetShipTypes()
		{
			HttpGet("/api/SEL/GetShipTypes", null, out APIShipType[] result);
			return result;
		}

		public APIRestrictionTypeException[] GetRestrictionTypeExceptions()
		{
			HttpGet("/api/SEL/GetShipRestrictionGroupExceptions", null, out APIRestrictionTypeException[] result);
			return result;
		}

		public void SetUpdateFinished(int monthId)
		{
			NameValueCollection postData = new NameValueCollection(1);
			postData.Add("month", monthId.ToString());
			HttpSet("/api/SEL/NotifyUpdateFinished", postData);
		}

		public void UpdateRasterImage(string layerName, byte[] imageBuffer)
		{
			NameValueCollection postData = new NameValueCollection(2);
			postData.Set("layer_name", layerName);
			postData.Set("image_data", Convert.ToBase64String(imageBuffer));
			HttpSet("/api/layer/UpdateRaster", postData);
		}

		public void SetKpiValues(string kpiName, int kpiValue, string kpiCategory, string kpiUnit, int country)
		{
			NameValueCollection postData = new NameValueCollection();
			postData.Set("name", kpiName);
			postData.Set("value", kpiValue.ToString());
			postData.Set("type", kpiCategory);
			postData.Set("unit", kpiUnit);
			if (country >= 0)
			{
				postData.Set("country", country.ToString());
			}
			HttpSet("/api/kpi/post", postData);
		}

		public void BatchPostKPI(IEnumerable<APIKPIResult> results)
		{
			string encodedKPIs = JsonConvert.SerializeObject(results);
			NameValueCollection postData = new NameValueCollection();
			postData.Set("kpiValues", encodedKPIs);
			HttpSet("/api/kpi/BatchPost", postData);
		}

		public void BatchPostIssues(IEnumerable<APIShippingIssue> shippingIssues)
		{
			string encodedIssues = JsonConvert.SerializeObject(shippingIssues);
			NameValueCollection postData = new NameValueCollection();
			postData.Set("issues", encodedIssues);
			HttpSet("/api/warning/SetShippingIssues", postData);
		}

		public void SetShippingIntensityValues(IEnumerable<KeyValuePair<int, int>> perGeometryIntensityValues)
		{
			string encodedIds = JsonConvert.SerializeObject(perGeometryIntensityValues);
			NameValueCollection postData = new NameValueCollection();
			postData.Set("values", encodedIds);
			HttpSet("/api/SEL/SetShippingIntensityValues", postData);
		}

		public void ReportErrorMessage(EErrorSeverity severity, string errorMessage, string stackTrace)
		{
			NameValueCollection postData = new NameValueCollection();
			postData.Set("source", "SEL");
			postData.Set("severity", severity.ToString());
			postData.Set("message", errorMessage);
			postData.Set("stack_trace", stackTrace);
			HttpSet("/api/log/Event", postData);
		}

		private bool HttpGet<TTargetType>(string apiEndPoint, NameValueCollection postValues, out TTargetType result)
		{
			return APIRequest.Perform(SELConfig.Instance.GetAPIRoot(), apiEndPoint, m_accessToken, postValues, out result);
		}

		private void HttpSet(string apiEndPoint, NameValueCollection postValues)
		{
			if (!APIRequest.Perform(SELConfig.Instance.GetAPIRoot(), apiEndPoint, m_accessToken, postValues))
			{
				Console.WriteLine($"APIRequest to {apiEndPoint} failed.");
			}
		}

		public static APIShippingPortIntensity[] DebugGenerateIntensityDataForAllPorts(IApiConnector apiProvider)
		{
			List<APIShippingPortIntensity> result = new List<APIShippingPortIntensity>();
			APIShipType[] shipTypes = apiProvider.GetShipTypes();
			APIShippingPortGeometry[] shipPortGeometry = apiProvider.GetShippingPortGeometry();

			if (shipPortGeometry != null)
			{
				foreach (APIShippingPortGeometry port in shipPortGeometry)
				{
					APIShippingPortIntensity intensity = new APIShippingPortIntensity();
					intensity.port_id = port.port_id;
					foreach (APIShipType shipType in shipTypes)
					{
						APIShippingPortIntensity.ShipTypeIntensity shipTypeIntensity = new APIShippingPortIntensity.ShipTypeIntensity();
						shipTypeIntensity.ship_type_id = shipType.ship_type_id;
						shipTypeIntensity.start_time = 0;
						shipTypeIntensity.ship_intensity = shipType.ship_type_id * 100;

						if (intensity.ship_intensity_values == null)
						{
							intensity.ship_intensity_values = new APIShippingPortIntensity.ShipTypeIntensity[1] { shipTypeIntensity };
						}
						else
						{
							int newArraySize = (intensity.ship_intensity_values == null) ? 1 : intensity.ship_intensity_values.Length + 1;
							APIShippingPortIntensity.ShipTypeIntensity[] resultArray = new APIShippingPortIntensity.ShipTypeIntensity[newArraySize];
							Array.Copy(intensity.ship_intensity_values, resultArray, newArraySize - 1);
							resultArray[newArraySize - 1] = shipTypeIntensity;
							intensity.ship_intensity_values = resultArray;
						}
					}
					result.Add(intensity);
				}
			}

			return result.ToArray();
		}
	}
}
