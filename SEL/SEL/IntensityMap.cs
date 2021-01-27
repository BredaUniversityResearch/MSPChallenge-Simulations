using SEL.API;
using System;
using System.Drawing;
using System.IO;

namespace SEL
{
	class IntensityMap: IDisposable
	{
		private IntensityMapGraphic m_graphic;
		private APIHeatmapSettings m_heatmapSettings;

		private int m_outputSizeX;
		private int m_outputSizeY;

		private float m_drawScaleX;
		private float m_drawScaleY;

		public IntensityMap(int outputSizeX, int outputSizeY, APIHeatmapSettings heatmapSettings)
		{
			m_outputSizeX = outputSizeX;
			m_outputSizeY = outputSizeY;
			m_heatmapSettings = heatmapSettings;
			m_drawScaleX = (float)((1.0f / (heatmapSettings.bounds_max.x - heatmapSettings.bounds_min.x)) * (float)m_outputSizeX);
			m_drawScaleY = (float)((1.0f / (heatmapSettings.bounds_max.y - heatmapSettings.bounds_min.y)) * (float)m_outputSizeY);

			m_graphic = new IntensityMapGraphicFloat(outputSizeX, outputSizeY);
		}

		public void Dispose()
		{
			m_graphic.Dispose();
		} 

		public void RenderEdge(LaneEdge edge, int edgeIntensityInShipCalls)
		{
			m_graphic.RenderLine(TransformPoint(edge.from.position), TransformPoint(edge.to.position), edgeIntensityInShipCalls);
		}

		public void SaveFile(string filePath)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			m_graphic.SaveFile(filePath, m_heatmapSettings);
		}

		private Point TransformPoint(Vector2D position)
		{
			int x = (int)((position.x - m_heatmapSettings.bounds_min.x) * m_drawScaleX);
			int y = m_outputSizeY - (int)((position.y - m_heatmapSettings.bounds_min.y) * m_drawScaleY);
			return new Point(x, y);
		}
	}
}
