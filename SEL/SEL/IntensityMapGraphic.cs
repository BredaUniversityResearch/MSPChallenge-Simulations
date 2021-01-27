using SEL.API;
using System;
using System.Drawing;
using System.IO;
using SEL.SpatialMapping;
using SEL.Util;

namespace SEL
{
	class RasterBounds
	{
		private readonly AABB m_fullWorldBounds;

		public int Width => m_resolutionX;
		public int Height => m_resolutionY;

		private readonly int m_resolutionX;
		private readonly int m_resolutionY;

		private readonly float m_drawScaleX;
		private readonly float m_drawScaleY;

		public RasterBounds(AABB fullWorldBounds, int resolutionX, int resolutionY)
		{
			m_resolutionX = resolutionX;
			m_resolutionY = resolutionY;
			m_drawScaleX = (float)((1.0f / (fullWorldBounds.max.x - fullWorldBounds.min.x)) * (float)resolutionX);
			m_drawScaleY = (float)((1.0f / (fullWorldBounds.max.y - fullWorldBounds.min.y)) * (float)resolutionY);
			m_fullWorldBounds = fullWorldBounds;
		}

		public Point WorldToRasterSpace(Vector2D position, bool flipY = true)
		{
			double x = (position.x - m_fullWorldBounds.min.x) * m_drawScaleX;
			double y = (position.y - m_fullWorldBounds.min.y) * m_drawScaleY;
			Point result;
			if (flipY)
			{
				result = new Point((int)Math.Floor(x), (int)Math.Round(m_resolutionY - y));
			}
			else
			{
				//I'm not entirely sure why we need different rounding modes on the X axis but this works. If we don't round this differently we are off-by-one with the raster alignments.
				result = new Point((int)Math.Ceiling(x), (int)Math.Round(y));
			}

			return result;
		}
	}

	abstract class IntensityMapGraphic : IDisposable
	{
		public int Width => m_rasterBounds.Width;
		public int Height => m_rasterBounds.Height;

		protected readonly RasterBounds m_rasterBounds;

		protected IntensityMapGraphic(RasterBounds rasterBounds)
		{
			m_rasterBounds = rasterBounds;
		}

		public virtual void Dispose()
		{
		}

		public abstract void Clear();
		protected abstract void RenderLine(Point from, Point to, int intensityValue);
		
		public void RenderEdge(GeometryEdge edge, int edgeIntensityInShipCalls)
		{
			RenderLine(m_rasterBounds.WorldToRasterSpace(edge.m_from.position), m_rasterBounds.WorldToRasterSpace(edge.m_to.position), edgeIntensityInShipCalls);
		}

		public abstract int GetRasterValue(int x, int y);
		public abstract void SetRasterValue(int x, int y, int value);
		public abstract int[] GetRasterValues();

		public virtual void SaveFile(Stream targetStream, RasterOutputConfig outputConfig, IValueMapper<int, float> valueMapper)
		{
		}

		public virtual void OnRenderEdgesComplete(IntensityMapGraphic restrictionMask)
		{
		}
	}
}
