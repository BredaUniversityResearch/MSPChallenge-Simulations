using SEL.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using SEL.RasterizerLib;
using SEL.SpatialMapping;
using SEL.Util;

namespace SEL
{
	/// <summary>
	/// Utility class for maintaining all raster instances.
	/// </summary>
	class RasterOutputManager
	{
		private class DebugLinearValueMapping : IValueMapper<int, float>
		{
			private readonly int m_min;
			private readonly int m_max;

			public DebugLinearValueMapping(int min, int max)
			{
				m_min = min;
				m_max = max;
			}

			public float Map(int input)
			{
				return (float) (input - m_min) / (float) (m_max - m_min);
			}
		}

		private APIRiskmapSettings m_riskmapSettings = null;
		private APIHeatmapBleedConfig m_bleedConfiguration = null;

		private class RasterInstance
		{
			public ShipType m_shipType = null;
			public IntensityMapGraphic m_outputRaster = null;
			public EHeatmapType m_heatmapType = EHeatmapType.ShippingIntensity;

			public bool m_restrictionMapUpToDate = false;
			public IntensityMapGraphic m_restrictionRaster = null;
		}

		private List<RasterInstance> m_rasterInstances = new List<RasterInstance>();
		private List<RasterOutputInstance> m_rasterOutputInstances = new List<RasterOutputInstance>();

		private RasterOutputConfig m_fullResolutionOutputConfig;
		private RasterOutputConfig m_melOutputConfig;
		private AABB m_simulationArea;

		public void ImportSharedHeatmapSettings(APIHeatmapSettings sharedHeatmapSettings)
		{
			m_riskmapSettings = sharedHeatmapSettings.riskmap_settings;
			m_bleedConfiguration = sharedHeatmapSettings.bleed_config;
		}

		public void ImportRasterData(APIHeatmapOutputSettings[] heatmapOutputSettings, ShipTypeManager shipTypeManager)
		{
			RasterBounds rasterBounds = new RasterBounds(m_simulationArea, m_fullResolutionOutputConfig.m_fullResolutionX,
				m_fullResolutionOutputConfig.m_fullResolutionY);

			foreach (APIHeatmapOutputSettings setting in heatmapOutputSettings)
			{
				if (setting.raster_bounds != null)
				{
					VerifyRasterBoundSettings(setting);

					RasterOutputInstance outputInstance = new RasterOutputInstance(setting, (setting.output_for_mel) ? m_melOutputConfig : m_fullResolutionOutputConfig, rasterBounds);

					foreach (int shipTypeId in setting.ship_type_ids)
					{
						RasterInstance instance = m_rasterInstances.Find(obj => obj.m_shipType.ShipTypeId == shipTypeId);
						if (instance == null)
						{
							instance = new RasterInstance();
							instance.m_outputRaster = new IntensityMapGraphicFloat(rasterBounds, m_bleedConfiguration);
							instance.m_restrictionRaster = new IntensityMapGraphicFloat(rasterBounds, m_bleedConfiguration);
							instance.m_shipType = shipTypeManager.FindShipTypeById(shipTypeId);
							m_rasterInstances.Add(instance);
						}

						outputInstance.AddIncludedRaster(instance.m_outputRaster);
					}

					m_rasterOutputInstances.Add(outputInstance);
				}
				else
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						$"Ignoring heatmap for output file for layer: {setting.layer_name} due to NULL bounds. Has this layer been setup correctly and is it present in the database?");
				}
			}
		}

		private void VerifyRasterBoundSettings(APIHeatmapOutputSettings outputSetting)
		{
			if (!outputSetting.output_for_mel)
			{
				if (outputSetting.raster_bounds[0][0] != m_simulationArea.min.x ||
					outputSetting.raster_bounds[0][1] != m_simulationArea.min.y ||
					outputSetting.raster_bounds[1][0] != m_simulationArea.max.x ||
					outputSetting.raster_bounds[1][1] != m_simulationArea.max.y)
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						$"Heatmap for output file for Layer: {outputSetting.layer_name} has incorrect bounds. Have you tried reimporting the region?");
				}
			}
			else
			{
				if (outputSetting.raster_bounds[0][0] != m_melOutputConfig.m_subBounds.min.x ||
					outputSetting.raster_bounds[0][1] != m_melOutputConfig.m_subBounds.min.y ||
					outputSetting.raster_bounds[1][0] != m_melOutputConfig.m_subBounds.max.x ||
					outputSetting.raster_bounds[1][1] != m_melOutputConfig.m_subBounds.max.y)
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						$"Heatmap for output file for layer: {outputSetting.layer_name} has incorrect bounds. Have you tried reimporting the region?");
				}
			}
		}

		public Rect GetRasterizerBoundRect()
		{
			return new Rect((float)m_simulationArea.min.x, (float)m_simulationArea.max.y,
				(float)m_simulationArea.max.x, (float)m_simulationArea.min.y);
		}

		public void UpdateRestrictionMaps(RouteManager routeManager, ShipTypeManager shipTypeManager)
		{
			Rect rasterizerBounds = GetRasterizerBoundRect();

			foreach (RasterInstance entry in m_rasterInstances)
			{
				if (!entry.m_restrictionMapUpToDate)
				{
					entry.m_restrictionRaster.Clear();

					if (entry.m_heatmapType == EHeatmapType.Riskmap)
					{
						UpdateRestrictionMapForRiskmap(routeManager, entry, rasterizerBounds);
					}
					else
					{
						UpdateRestrictionMapForHeatmap(routeManager, shipTypeManager, entry, rasterizerBounds);
					}

					if (SELConfig.Instance.ShouldOutputRestrictionMaps())
					{
						using (FileStream fs = new FileStream("Output/Restriction_" + entry.m_shipType.ShipTypeName + ".png", FileMode.Create))
						{
							entry.m_restrictionRaster.SaveFile(fs, m_fullResolutionOutputConfig, new DebugLinearValueMapping(0, 500));
						}
					}

					entry.m_restrictionMapUpToDate = true;
				}
			}
		}

		private void UpdateRestrictionMapForRiskmap(RouteManager routeManager, RasterInstance entry, Rect rasterizerBounds)
		{
			BitArray excludedLayerIds = new BitArray(256);
			if (entry.m_heatmapType == EHeatmapType.Riskmap)
			{
				foreach (int excludedLayerId in m_riskmapSettings.restriction_layer_exceptions)
				{
					excludedLayerIds.Set(excludedLayerId, true);
				}
			}

			RasterizerSurface surface = new RasterizerSurface(entry.m_restrictionRaster.Width, entry.m_restrictionRaster.Height, entry.m_restrictionRaster.GetRasterValues());
			foreach (RestrictionMesh restrictionMesh in routeManager.GetRestrictionMeshes())
			{
				if (excludedLayerIds.Get(restrictionMesh.m_layerId) == false)
				{
					Rasterizer.RasterizePolygonsFlat(surface, restrictionMesh, 1000, rasterizerBounds);
				}
			}
		}

		private void UpdateRestrictionMapForHeatmap(RouteManager routeManager, ShipTypeManager shipTypeManager, RasterInstance entry, Rect rasterizerBounds)
		{
			int shipTypeMask = (1 << entry.m_shipType.ShipTypeId);

			RasterizerSurface surface = new RasterizerSurface(entry.m_restrictionRaster.Width, entry.m_restrictionRaster.Height, entry.m_restrictionRaster.GetRasterValues());
			foreach (RestrictionMesh restrictionMesh in routeManager.GetRestrictionMeshes())
			{
				//If something is blocked by our area then make sure to rasterize it.
				if ((restrictionMesh.m_restrictionType.GetAllowedShipTypeMask() & shipTypeMask) != shipTypeMask)
				{
					Rasterizer.RasterizePolygonsFlat(surface, restrictionMesh, 1000, rasterizerBounds);
				}
			}
		}

		public void UpdateOutputRasters(RouteIntensity[] allRouteIntensities, ShipTypeManager shipTypeManager, RouteManager routeManager, IApiConnector apiConnector)
		{
			List<string> updatedRasterLayerNames = new List<string>();

			UpdateRestrictionMaps(routeManager, shipTypeManager);
			RebuildIntensityRasters(routeManager, allRouteIntensities);
			
			foreach (RasterOutputInstance outputInstance in m_rasterOutputInstances)
			{
				using (MemoryStream stream = new MemoryStream())
				{
					outputInstance.CombineAndSave(stream);

					byte[] buffer = stream.ToArray();
					apiConnector.UpdateRasterImage(outputInstance.LayerName, buffer);
				}

				updatedRasterLayerNames.Add(outputInstance.LayerName);
			}
		}

		private void RebuildIntensityRasters(RouteManager routeManager, RouteIntensity[] allRouteIntensities)
		{
			foreach (RasterInstance entry in m_rasterInstances)
			{
				entry.m_outputRaster.Clear();
				if (entry.m_heatmapType == EHeatmapType.ShippingIntensity)
				{
					RebuildIntensityRaster(entry, allRouteIntensities, routeManager);
				}
				else if (entry.m_heatmapType == EHeatmapType.Riskmap)
				{
					RebuildRiskmapRaster(entry, allRouteIntensities, routeManager);
				}
				else
				{
					throw new ArgumentException("Unknown heatmap type {0}", entry.m_heatmapType.ToString());
				}

				//entry.m_outputRaster.SaveFile("Output/" + entry.m_shipType.ShipTypeName + "_Raster.png",
				//	m_fullResolutionOutputConfig, new DebugLinearValueMapping(0, 500));
			}
		}

		private void RebuildIntensityRaster(RasterInstance entry, IEnumerable<RouteIntensity> routeIntensities, RouteManager routeManager)
		{
			foreach (RouteIntensity route in routeIntensities)
			{
				if (entry.m_shipType.ShipTypeId == route.ShipTypeId)
				{
					Route cachedRoute = routeManager.FindCachedRoute(route);
					if (cachedRoute != null)
					{
						foreach (LaneEdge edge in cachedRoute.GetRouteEdges())
						{
							entry.m_outputRaster.RenderEdge(edge, route.Intensity);
						}
					}
				}
			}
			if (SELConfig.Instance.ShouldOutputRestrictionMaps())
			{
				using (FileStream fs = new FileStream("Output/UnboundedMap_" + entry.m_shipType.ShipTypeName + ".png", FileMode.Create))
				{
					entry.m_outputRaster.SaveFile(fs, m_fullResolutionOutputConfig, new DebugLinearValueMapping(0, 500));
				}
			}

			entry.m_outputRaster.OnRenderEdgesComplete(entry.m_restrictionRaster);
		}

		private void RebuildRiskmapRaster(RasterInstance entry, IEnumerable<RouteIntensity> routeIntensities, RouteManager routeManager)
		{
			RebuildIntensityRaster(entry, routeIntensities, routeManager);

			for (int y = 0; y < entry.m_outputRaster.Height; ++y)
			{
				for (int x = 0; x < entry.m_outputRaster.Width; ++x)
				{
					int riskValue = 0;
					int baseIntensity = entry.m_outputRaster.GetRasterValue(x, y);
					if (baseIntensity > 0)
					{
						int restrictionValue = (entry.m_restrictionRaster.GetRasterValue(x, y) != 0)? 1 : 0;
						int intensityOverflow = Math.Max(0, baseIntensity - m_riskmapSettings.shipping_intensity_risk_value);
						riskValue = intensityOverflow + (baseIntensity * restrictionValue);
					}

					entry.m_outputRaster.SetRasterValue(x, y, riskValue);
				}
			}
		}

		public IntensityMapGraphic[] GetLatestResultsForType(EHeatmapType heatmapType)
		{
			List<IntensityMapGraphic> result = new List<IntensityMapGraphic>(m_rasterInstances.Count);
			foreach (RasterInstance instance in m_rasterInstances)
			{
				if (instance.m_heatmapType == heatmapType)
				{
					result.Add(instance.m_outputRaster);
				}
			}
			return result.ToArray();
		}

		public void SetOutputConfiguration(APIAreaOutputConfiguration outputAreaConfiguration)
		{
			m_simulationArea = outputAreaConfiguration.simulation_area.ToAABB();

			AABB melBounds;
			double cellSize;
			if (outputAreaConfiguration.mel_area != null)
			{
				melBounds = outputAreaConfiguration.mel_area.ToAABB();
				cellSize = (outputAreaConfiguration.mel_cell_size / (double) outputAreaConfiguration.pixels_per_mel_cell);
			}
			else
			{
				melBounds = m_simulationArea;
				cellSize = outputAreaConfiguration.simulation_area_cell_size;
			}

			int fullResolutionX = (int) (outputAreaConfiguration.simulation_area.GetWidth() / cellSize);
			int fullResolutionY = (int) (outputAreaConfiguration.simulation_area.GetHeight() / cellSize);

			m_fullResolutionOutputConfig = new RasterOutputConfig(fullResolutionX, fullResolutionY);
			m_melOutputConfig = new RasterOutputConfig(fullResolutionX, fullResolutionY,
				outputAreaConfiguration.mel_resolution_x, outputAreaConfiguration.mel_resolution_y,
				melBounds);
		}

		public Vector2 GetFullOutputResolution()
		{
			return new Vector2(m_fullResolutionOutputConfig.m_fullResolutionX, m_fullResolutionOutputConfig.m_fullResolutionY);
		}
	}
}
