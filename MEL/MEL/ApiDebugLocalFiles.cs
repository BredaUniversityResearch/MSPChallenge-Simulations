using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using EwEMSPLink;
using Newtonsoft.Json.Linq;

namespace MEL
{
	/// <summary>
	/// API Connector used for debugging.
	/// This API connector write return the calculated data to disk in a known folder.
	/// </summary>
	public class ApiDebugLocalFiles: IApiConnector
	{
		private const string DebugDataFolder = "DebugData/";
		private readonly string m_ConfigFileName;

		public bool ShouldRasterizeLayers => false;
		private int m_CurrentGameMonth = -1;

		private Fishing[] m_FishingValues = null;

		public ApiDebugLocalFiles(string configFileName)
		{
			m_ConfigFileName = configFileName;
		}

		public bool CheckApiAccess()
		{
			return true;
		}
		public int ApiRequestCurrentMonth()
		{
			return m_CurrentGameMonth;
		}

		public int GetCurrentGameMonth()
		{
			return m_CurrentGameMonth;
		}
		public string GetServerUrl()
		{
			throw new System.NotImplementedException();
		}
		public string GetAccessToken()
		{
			throw new System.NotImplementedException();
		}

		public void SetInitialFishingValues(List<cScalar> fishingValues)
		{
			m_FishingValues = new Fishing[fishingValues.Count];
			for (int i = 0; i < fishingValues.Count; ++i)
			{
				m_FishingValues[i] = new Fishing { name = fishingValues[i].Name, scalar = (float)fishingValues[i].Value };
			}
		}

		public string? GetMelConfigAsString()
		{
			var configValues = JObject.Parse(File.ReadAllText(Path.Combine(DebugDataFolder, m_ConfigFileName + ".json")));
			var dataModel = configValues["datamodel"] as JObject;
			if (dataModel == null) return null;
			var melConfig = dataModel["MEL"] as JObject;
			if (melConfig == null) return null;

			var pressures = melConfig["pressures"] as JArray ?? new JArray();
			foreach (var pressure in pressures)
			{
			    var policyFilters = pressure["policy_filters"];
			    if (policyFilters == null)
			    {
			        continue;
			    }

			    var layers = pressure["layers"] as JArray ?? new JArray();
			    foreach (var pressureLayer in layers)
			    {
			        var layerName = pressureLayer["name"]?.ToString();
			        if (string.IsNullOrEmpty(layerName))
			        {
			            continue;
			        }

			        var metaLayers = dataModel["meta"] as JArray ?? new JArray();
			        var layer = metaLayers.FirstOrDefault(l => l["layer_name"]?.ToString() == layerName);
			        if (layer == null)
			        {
			            continue;
			        }

			        var layerInfoProperties = layer["layer_info_properties"] as JArray ?? new JArray();
			        var layerInfoPolicyTypeProps = layerInfoProperties
			            .Where(p => p["policy_type"] != null)
			            .ToList();
			        if (!layerInfoPolicyTypeProps.Any())
			        {
			            continue;
			        }

			        pressureLayer["policy_filters"] = policyFilters;
			    }
			}

			return dataModel["MEL"]?.ToString();
		}

		public string[] GetUpdatedLayers()
		{
			return new string[0];
		}

		public Fishing[] GetFishingValuesForMonth(int month)
		{
			return m_FishingValues;
		}

		public void SubmitKpi(string kpiName, int kpiMonth, double kpiValue, string kpiUnits)
		{
			//Nothing
		}

		public void NotifyTickDone()
		{
			++m_CurrentGameMonth;
		}

		public double[,]? GetRasterLayerByName(string? layerName)
		{
			throw new System.NotImplementedException();
		}

		public void SubmitRasterLayerData(string layerName, Bitmap rasterImage)
		{
			using Stream fs = File.OpenWrite(Path.Combine(DebugDataFolder, m_ConfigFileName,
				MEL.ConvertLayerName(layerName) + ".tif"));
#pragma warning disable CA1416 // Validate platform compatibility
			rasterImage.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
 #pragma warning restore CA1416
		}

		public APILayerGeometryData? GetLayerData(
			string? layerName,
            int layerType,
            bool constructionOnly,
			JObject? policyFilters = null
		) {
			return null;
		}

		public double[,]? GetRasterizedPressure(string name)
		{
			double[,]? result = null;
			using Stream stream = File.OpenRead(Path.Combine(DebugDataFolder, m_ConfigFileName, MEL.ConvertLayerName(name)+".tif"));
 #pragma warning disable CA1416 // Validate platform compatibility
			using Bitmap bitmap = new (stream);
 #pragma warning restore CA1416
			result = Rasterizer.PNGToArray(bitmap, 1.0f, MEL.x_res, MEL.y_res);

			return result;
		}

		public void UpdateAccessToken(string newAccessToken)
		{
			//Moot. I don't think this should ever be called with a local files api.
			throw new NotImplementedException();
		}

		public void UpdateMonth(int month)
		{
			m_CurrentGameMonth = month;
		}

		public int[] GetEcoFleets()
		{
			return Array.Empty<int>();
		}
	}
}
