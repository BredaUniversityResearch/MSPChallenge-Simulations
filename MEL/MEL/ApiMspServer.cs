using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using EwEMSPLink;
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
	public class ApiMspServer: ApiConnectorBase, IApiConnector
	{
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		// ReSharper disable once ClassNeverInstantiated.Local
		private class APIGetRasterResponse
		{
			//public string displayed_bounds;
			public string image_data = null;
		}

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
		}

		public bool ShouldRasterizeLayers => true;

		public ApiMspServer(string mspServerAddress) : base(mspServerAddress)
		{
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

		public string? GetMelConfigAsString()
		{
			return HttpGet("/api/mel/config", out JToken result) ? result.ToString() : null;
		}

		public string[] GetUpdatedLayers()
		{
			return HttpGet("/api/mel/Update", out string[] result) ? result : new string[0];
		}

		public Fishing[] GetFishingValuesForMonth(int month)
		{
			if (month == 0)
			{
				//When the setup ends we are still in lastupdatedmonth 0. We need to get the fishing values for month -1 then to account for starting plans.
				//I am so sorry for this.
				month = -1;
			}

			NameValueCollection postValues = new(1);
			postValues.Add("game_month", month.ToString());
			return HttpGet("/api/mel/GetFishing", postValues, out Fishing[] result) ? result : new Fishing[0];

		}

		public void SubmitKpi(string kpiName, int kpiMonth, double kpiValue, string kpiUnits)
		{
			NameValueCollection values = new() {
				{ "name" , kpiName },
				{ "month", kpiMonth.ToString(CultureInfo.InvariantCulture) },
				{ "value" , kpiValue.ToString(CultureInfo.InvariantCulture) },
				{ "type" , "ECOLOGY" },
				{ "unit" , kpiUnits }
			};
			HttpGet("/api/kpi/post", values, out int _);
		}

		public void NotifyTickDone()
		{
			HttpSet("/api/mel/TickDone");
		}

		public double[,]? GetRasterLayerByName(string? layerName)
		{
			NameValueCollection postData = new NameValueCollection(1);
			postData.Set("layer_name", layerName);
			if (!HttpGet("/api/layer/GetRaster", postData, out APIGetRasterResponse apiResponse))
				return null;
			byte[] imageBytes = Convert.FromBase64String(apiResponse.image_data);
			using Stream stream = new MemoryStream(imageBytes);
			using Bitmap bitmap = new(stream);
			return Rasterizer.PNGToArray(bitmap, 1.0f, MEL.x_res, MEL.y_res);
		}

		public void SubmitRasterLayerData(string layerName, Bitmap rasterImage)
		{
			using MemoryStream stream = new(16384);
			rasterImage.Save(stream, ImageFormat.Png);
			NameValueCollection postData = new NameValueCollection(2);
			postData.Set("layer_name", MEL.ConvertLayerName(layerName));
			postData.Set("image_data", Convert.ToBase64String(stream.ToArray()));
			HttpSet("/api/layer/UpdateRaster", postData);
		}

		public APILayerGeometryData? GetLayerData(string? layerName, int layerType, bool constructionOnly)
		{
			NameValueCollection? values = new() {
				{"name", layerName },
				{"layer_type", layerType.ToString() },
				{"construction_only", constructionOnly ? "1" : "0" }
			};

			return HttpGet("/api/mel/GeometryExportName", values, out APILayerGeometryData? result) ? result : null;
		}

		public double[,]? GetRasterizedPressure(string name)
		{
			throw new NotImplementedException();
		}
	}
}