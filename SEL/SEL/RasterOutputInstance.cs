using System.Collections.Generic;
using System.IO;
using SEL.API;

namespace SEL
{
	class RasterOutputInstance
	{
		private readonly APIHeatmapOutputSettings m_outputSettings;
		private readonly RasterOutputConfig m_outputConfig;
		private readonly RasterBounds m_rasterBounds;
		private List<IntensityMapGraphic> m_includedRasters = new List<IntensityMapGraphic>();

		public string LayerName => m_outputSettings.layer_name;

		public RasterOutputInstance(APIHeatmapOutputSettings outputSettings, RasterOutputConfig outputConfig, RasterBounds rasterBounds)
		{
			m_outputSettings = outputSettings;
			m_outputConfig = outputConfig;
			m_rasterBounds = rasterBounds;
		}

		public void AddIncludedRaster(IntensityMapGraphic raster)
		{
			m_includedRasters.Add(raster);
		}

		public void CombineAndSave(Stream targetStream)
		{
			IntensityMapGraphicFloat combinedValues = new IntensityMapGraphicFloat(m_rasterBounds, null);
			foreach(IntensityMapGraphic raster in m_includedRasters)
			{
				combinedValues.AddValuesFrom((IntensityMapGraphicFloat)raster);
			}

			combinedValues.SaveFile(targetStream, m_outputConfig, m_outputSettings);
		}
	}
}