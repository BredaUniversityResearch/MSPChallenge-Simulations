using System;
using System.IO;
using SkiaSharp;

namespace REL
{
	public class HeatmapDataGrid
	{
		private readonly int m_width;
		private readonly int m_height;
		private readonly float[] m_outputData;

		public HeatmapDataGrid(int a_outputWidth, int a_outputHeight)
		{
			m_width = a_outputWidth;
			m_height = a_outputHeight;
			m_outputData = new float[a_outputWidth * a_outputHeight];
		}

		public void AdditivePlot(float a_x, float a_y, float a_value)
		{
			float fractionX = a_x % 1.0f;
			float fractionY = a_y % 1.0f;

			int flooredX = (int)Math.Floor(a_x);
			int flooredY = (int) Math.Floor(a_y);

			AddValueToCell(flooredX, flooredY, a_value * ((1.0f - fractionX) * (1.0f - fractionY)));
			AddValueToCell(flooredX + 1, flooredY, a_value * (fractionX * (1.0f - fractionY)));
			AddValueToCell(flooredX, flooredY + 1, a_value * ((1.0f - fractionX) * fractionY));
			AddValueToCell(flooredX + 1, flooredY + 1, a_value * (fractionX * fractionY));
		}

		private void AddValueToCell(int a_x, int a_y, float a_valueToAdd)
		{
			if (a_x >= 0 && a_x < m_width && a_y >= 0 && a_y < m_height)
			{
				m_outputData[a_x + (a_y * m_width)] += a_valueToAdd;
			}
		}

		public void WriteImageAsPngToStream(Stream a_targetStream)
		{
			using SKBitmap image = new(m_width, m_height, SKColorType.Bgra8888, SKAlphaType.Opaque);
			for (int y = 0; y < m_height; ++y)
			{
				for (int x = 0; x < m_width; ++x)
				{
					float mappedValue = m_outputData[x + (y * m_width)];
					byte mappedColourValue = (byte)(mappedValue * 255.0f);
					image.SetPixel(x, y, new SKColor(mappedColourValue, mappedColourValue, mappedColourValue, 255));
				}
			}

			using SKImage skImage = SKImage.FromBitmap(image);
			using SKData encoded = skImage.Encode(SKEncodedImageFormat.Png, 100);
			encoded.SaveTo(a_targetStream);
		}

		public void PlotLine(Vector2D a_from, Vector2D a_to, float a_value)
		{
			DrawLineBresenhams((int)a_from.x, (int)a_from.y, (int)a_to.x, (int)a_to.y, a_value);
		}

		private void DrawLineBresenhams(int a_x, int a_y, int a_x2, int a_y2, float a_value)
		{
			if ((a_x < 0 && a_x2 < 0) || (a_x >= m_width && a_x2 >= m_width) ||
			    (a_y < 0 && a_y2 < 0) || (a_y >= m_height && a_y2 >= m_height))
			{
				return;
			}

			if (a_y > a_y2)
			{
				int yTemp = a_y;
				a_y = a_y2;
				a_y2 = yTemp;

				int xTemp = a_x;
				a_x = a_x2;
				a_x2 = xTemp;
			}

			int w = a_x2 - a_x;
			int h = a_y2 - a_y;

			int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
			if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
			if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
			if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
			int longest = Math.Abs(w);
			int shortest = Math.Abs(h);
			if (!(longest > shortest))
			{
				longest = Math.Abs(h);
				shortest = Math.Abs(w);
				if (h < 0) dy2 = -1;
				else if (h > 0) dy2 = 1;
				dx2 = 0;
			}

			int numerator = longest >> 1;
			for (int i = 0; i < longest; i++)
			{
				AdditivePlot(a_x, a_y, a_value);

				numerator += shortest;
				if (!(numerator < longest))
				{
					numerator -= longest;
					a_x += dx1;
					a_y += dy1;
				}
				else
				{
					a_x += dx2;
					a_y += dy2;
				}
			}
		}
	}
}
