using SEL.API;
using SEL.SpatialMapping;
using SEL.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace SEL
{
	/// <summary>
	/// Custom graphics class that uses a floating point representation for each pixel value to allow for more precision.
	/// The GDI drawing api only allows for a minimum alpha value of 1/255 where here we can convert the values after we have drawn everything
	/// </summary>
	class IntensityMapGraphicFloat : IntensityMapGraphic
	{
		private int[] m_intensityRaster;
		private AABB m_outputBounds;

		private APIHeatmapBleedConfig m_bleedConfig;
		private IntensityMapBleedMatrixCollection m_bleedMatrixCollection;

		public IntensityMapGraphicFloat(RasterBounds rasterBounds, APIHeatmapBleedConfig bleedConfig)
			: base(rasterBounds)
		{
			m_intensityRaster = new int[rasterBounds.Width * rasterBounds.Height];
			m_outputBounds = new AABB(new Vector2D(0.0, 0.0), new Vector2D((double)rasterBounds.Width, (double)rasterBounds.Height));
			m_bleedConfig = bleedConfig;
			m_bleedMatrixCollection = new IntensityMapBleedMatrixCollection(m_bleedConfig?.bleed_number_of_kernels ?? 1);
		}

		public override void Clear()
		{
			for (int i = 0; i < Width * Height; ++i)
			{
				m_intensityRaster[i] = 0;
			}
		}

		public void AddValuesFrom(IntensityMapGraphicFloat other)
		{
			for (int i = 0; i < m_intensityRaster.Length; ++i)
			{
				m_intensityRaster[i] += other.m_intensityRaster[i];
			}
		}

		protected override void RenderLine(Point from, Point to, int intensityValue)
		{
			if (LineClipping.ClipLinePoints(ref from, ref to, m_outputBounds))
			{
				DrawLineBresenhams(from.X, from.Y, to.X, to.Y, intensityValue);
			}
		}

		public override int GetRasterValue(int x, int y)
		{
			return m_intensityRaster[x + (y * Width)];
		}

		public override void SetRasterValue(int x, int y, int value)
		{
			m_intensityRaster[x + (y * Width)] = value;
		}

		public override int[] GetRasterValues()
		{
			return m_intensityRaster;
		}

		public override void OnRenderEdgesComplete(IntensityMapGraphic restrictionMask)
		{
			base.OnRenderEdgesComplete(restrictionMask);

			const int BLUR_PASSES = 1;

			int[] inputValues = m_intensityRaster;
			for (int i = 0; i < BLUR_PASSES; ++i)
			{
				int[] outputValues = new int[m_intensityRaster.Length];
				BleedOverflowValues(Width, Height, inputValues, outputValues, m_bleedConfig.bleed_treshold, restrictionMask);
				inputValues = outputValues;
			}
			m_intensityRaster = inputValues;
		}

		private void DrawLineBresenhams(int x, int y, int x2, int y2, int intensityValue)
		{
			if ((x < 0 && x2 < 0) || (x >= Width && x2 >= Width) ||
				(y < 0 && y2 < 0) || (y >= Height && y2 >= Height))
			{
				return;
			}

			if (y > y2)
			{
				int yTemp = y;
				y = y2;
				y2 = yTemp;

				int xTemp = x;
				x = x2;
				x2 = xTemp;
			}

			int w = x2 - x;
			int h = y2 - y;

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
				AdditivePlot(x, y, intensityValue);

				numerator += shortest;
				if (!(numerator < longest))
				{
					numerator -= longest;
					x += dx1;
					y += dy1;
				}
				else
				{
					x += dx2;
					y += dy2;
				}
			}
		}

		private void AdditivePlot(int x, int y, int value)
		{
			m_intensityRaster[x + (y * Width)] += value;
		}

		private float[,] CreateMaskedKernel(int overflowBoundary, int overflowValue, APIHeatmapBleedConfig settings,
			IntensityMapGraphic restrictionMask, int maskX, int maskY)
		{
			float[,] selectedKernel = SelectBleedingMatrixForOverflowValue(overflowBoundary, overflowValue, settings);
			int kernelOffsetX = -((selectedKernel.GetLength(0) - 1) / 2);
			int kernelOffsetY = -((selectedKernel.GetLength(1) - 1) / 2);

			float weightToRebalance = 0.0f;
			int unmaskedKernelCells = (selectedKernel.GetLength(0) * selectedKernel.GetLength(1)) - 1; //We don't want the center kernel cell to be taken into account for this.
			bool[,] kernelMask = new bool[selectedKernel.GetLength(0), selectedKernel.GetLength(1)];

			//Create the kernel mask by comparing it to the restriction mask that we have.
			for (int kernelY = 0; kernelY < selectedKernel.GetLength(0); ++kernelY)
			{
				for (int kernelX = 0; kernelX < selectedKernel.GetLength(1); ++kernelX)
				{
					int currentMaskX = maskX + (kernelX + kernelOffsetX);
					int currentMaskY = maskY + (kernelY + kernelOffsetY);

					if (currentMaskX >= 0 && currentMaskX < restrictionMask.Width && 
						currentMaskY >= 0 && currentMaskY < restrictionMask.Height)
					{
						if (restrictionMask.GetRasterValue(currentMaskX, currentMaskY) != 0)
						{
							kernelMask[kernelX, kernelY] = true;
							weightToRebalance += selectedKernel[kernelX, kernelY];
							--unmaskedKernelCells;
						}
					}
				}
			}

			float[,] result;
			if (unmaskedKernelCells <= 0)
			{
				//All kernel cells are masked, return NULL so we know we should not use this kernel.
				result = null;
			}
			else if (weightToRebalance > 0.0f)
			{
				result = new float[selectedKernel.GetLength(0), selectedKernel.GetLength(1)];
				float weightPerCell = weightToRebalance / unmaskedKernelCells;
				for (int kernelY = 0; kernelY < selectedKernel.GetLength(0); ++kernelY)
				{
					for (int kernelX = 0; kernelX < selectedKernel.GetLength(1); ++kernelX)
					{
						if (kernelMask[kernelX, kernelY] == false)
						{
							result[kernelX, kernelY] = selectedKernel[kernelX, kernelY] + weightPerCell;
						}
					}
				}
			}
			else
			{
				result = selectedKernel;
			}

			return result;
		}

		/// <summary>
		/// Will bleed all values that exceed the overflow boundary to nearby cells 
		/// </summary>
		/// <param name="rasterSizeX"></param>
		/// <param name="rasterSizeY"></param>
		/// <param name="inputValues"></param>
		/// <param name="outputValues"></param>
		/// <param name="overflowBoundary"></param>
		private void BleedOverflowValues(int rasterSizeX, int rasterSizeY, int[] inputValues, int[] outputValues, int overflowBoundary, IntensityMapGraphic restrictionMask)
		{
			for (int y = 0; y < rasterSizeY; ++y)
			{
				for (int x = 0; x < rasterSizeX; ++x)
				{
					int currentRasterCellIndex = x + (y * rasterSizeX);
					int currentValue = inputValues[currentRasterCellIndex];
					if (currentValue > overflowBoundary)
					{
						int overflowValue = currentValue - overflowBoundary;
						//Set the unoverflowed value;
						outputValues[currentRasterCellIndex] += overflowBoundary;

						float[,] selectedKernel =
							CreateMaskedKernel(overflowBoundary, overflowValue, m_bleedConfig, restrictionMask, x, y);
						if (selectedKernel == null)
						{
							continue;
						}

						int kernelOffsetX = -((selectedKernel.GetLength(0) - 1) / 2);
						int kernelOffsetY = -((selectedKernel.GetLength(1) - 1) / 2);

						for (int kernelY = 0; kernelY < selectedKernel.GetLength(0); ++kernelY)
						{
							for (int kernelX = 0; kernelX < selectedKernel.GetLength(1); ++kernelX)
							{
								int outputX = x + (kernelX + kernelOffsetX);
								int outputY = y + (kernelY + kernelOffsetY);
								if (outputX >= 0 && outputX < rasterSizeX && outputY >= 0 && outputY < rasterSizeY)
								{
									outputValues[outputX + (outputY * rasterSizeX)] += (int)((float)overflowValue * selectedKernel[kernelX, kernelY]);
								}
							}
						}
					}
					else
					{
						outputValues[currentRasterCellIndex] += currentValue;
					}
				}
			}
		}

		private float[,] SelectBleedingMatrixForOverflowValue(int overflowBoundary, int overflowValue, APIHeatmapBleedConfig settings)
		{
			float overflowPercentage = (float)overflowValue / (float)overflowBoundary;
			int bleedMatrixIndex = (int)Math.Round(Math.Pow(overflowPercentage, settings.bleed_overflow_curve_power) * settings.bleed_overflow_curve_multiplier);
			return m_bleedMatrixCollection.GetBleedingKernel(bleedMatrixIndex);
		}

		public override void SaveFile(Stream targetStream, RasterOutputConfig outputConfig, IValueMapper<int, float> valueMapper)
		{
			int stride = (Width + 3) & ~0x3; //Round up to a multiple of 4
			byte[] colourBits = new byte[stride * Height];
			GCHandle colourBitsHandle = GCHandle.Alloc(colourBits, GCHandleType.Pinned);

			using (Bitmap image = new Bitmap(Width, Height, stride, PixelFormat.Format8bppIndexed, colourBitsHandle.AddrOfPinnedObject()))
			{
				//Build a grayscale colour palette.
				ColorPalette palette = image.Palette;
				for (int i = 0; i < 255; ++i)
				{
					palette.Entries[i] = Color.FromArgb(255, i, i, i);
				}
				image.Palette = palette;

				for (int y = 0; y < Height; ++y)
				{
					for (int x = 0; x < Width; ++x)
					{
						float mappedValue = Math.Max(0.0f, Math.Min(valueMapper.Map(m_intensityRaster[x + (y * Width)]), 1.0f));
						byte mappedColourValue = (byte)(mappedValue * 255.0f);
						colourBits[x + (y * stride)] = mappedColourValue;
					}
				}

				if (outputConfig.m_outputResolutionX != -1 && outputConfig.m_outputResolutionY != -1)
				{
					//need to resize to output_size_x & y
					using (Bitmap resizedImage = new Bitmap(outputConfig.m_outputResolutionX, outputConfig.m_outputResolutionY, PixelFormat.Format32bppArgb))
					{
						using (Graphics resizedGraphic = Graphics.FromImage(resizedImage))
						{
							//We need to do a weird transformation to swap the offsets since our coordinate systems are mismatching.
							Point transformedMin = m_rasterBounds.WorldToRasterSpace(outputConfig.m_subBounds.min, false);
							Point transformedMax = m_rasterBounds.WorldToRasterSpace(outputConfig.m_subBounds.max, false);

							Size originalSize = new Size(Width, Height);
							Size deltaSizeMax = originalSize - new Size(transformedMin);
							Size deltaSizeMin = originalSize - new Size(transformedMax);
							
							Size sourceSize = deltaSizeMax - deltaSizeMin;
							Rectangle sourceRect = new Rectangle(new Point(transformedMin.X, deltaSizeMin.Height), sourceSize); 

							resizedGraphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
							resizedGraphic.DrawImage(image,
								new Rectangle(0, 0, outputConfig.m_outputResolutionX, outputConfig.m_outputResolutionY), sourceRect,
								GraphicsUnit.Pixel);
						}

						resizedImage.Save(targetStream, ImageFormat.Png);
					}
				}
				else
				{
					image.Save(targetStream, ImageFormat.Png);
				}
			}

			colourBitsHandle.Free();
		}
	}
}
