using System;
using System.Collections.Generic;
using MSWSupport;
using Newtonsoft.Json.Linq;

namespace MEL
{
	/// <summary>
	/// A layer represents a set of geometry point grouped together by purpose.
	/// Examples are wind farms which contain multiple geometries of different types, but only geometry that should be interpreted as wind farms.
	/// This class stores all the relevant data to the layer such as the geometry and a rasterized representation.
	/// </summary>
	public class RasterizedLayer
	{
		public readonly string? name;
		public readonly int LayerType;
		public readonly bool constructionOnly;
		public readonly JObject? policyFilters;

		public bool IsLoadedCorrectly { get; private set; }
		public double[,]? rawData;

		public RasterizedLayer(LayerData layerData)
		{
			name = layerData.layer_name;
			LayerType = layerData.layer_type;
			constructionOnly = layerData.construction;
			IsLoadedCorrectly = false;
			policyFilters = layerData.policy_filters;
		}

		public void GetLayerDataAndRasterize(MEL mel)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			APILayerGeometryData? layerGeometryData = mel.ApiConnector.GetLayerData(name, LayerType, constructionOnly, policyFilters);

			if (layerGeometryData != null)
			{
				List<APILayerGeometryData?> g = new List<APILayerGeometryData?> { layerGeometryData };

				switch (layerGeometryData.geotype)
				{
				case "polygon":
					rawData = Rasterizer.RasterizePolygons(g, 1.0, 1, MEL.x_res, MEL.y_res, new Rect(mel.x_min, mel.y_min, mel.x_max, mel.y_max));
					IsLoadedCorrectly = g.Count > 0;
					break;
				case "line":
					rawData = Rasterizer.RasterizeLines(g, 1.0, 1, MEL.x_res, MEL.y_res, new Rect(mel.x_min, mel.y_min, mel.x_max, mel.y_max));
					IsLoadedCorrectly = g.Count > 0;
					break;
				case "point":
					rawData = Rasterizer.RasterizePoints(g, 1.0, MEL.x_res, MEL.y_res, new Rect(mel.x_min, mel.y_min, mel.x_max, mel.y_max));
					IsLoadedCorrectly = g.Count > 0;
					break;
				case "raster":
					try
					{
						rawData = mel.ApiConnector.GetRasterLayerByName(name);
						if (rawData == null)
						{
							throw new Exception("Got null raw data from API. This can happen when this layer is an output from a different simulation.");
						}

						IsLoadedCorrectly = true;
					}
					catch (Exception e)
					{
						ConsoleLogger.Error($"{name} could not be loaded. Pressure layers will not be generated accurately! \nException: {e.Message}");
					}

					break;
				}
			}
			else
			{
				ConsoleLogger.Warning($"{name} does not exist or does not have geometry");
			}
			
			watch.Stop();
			ConsoleLogger.Info("Got: " + name + (LayerType == -1 ? "" : "|" + LayerType) + (policyFilters == null ? "" : " with " + policyFilters) + ", IsLoadedCorrectly: " + IsLoadedCorrectly + ", load time: " + watch.ElapsedMilliseconds);
		}
	}
}