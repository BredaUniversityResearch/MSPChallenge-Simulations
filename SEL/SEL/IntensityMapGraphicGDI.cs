/* Deprecated as of 26-09-2017 in favour of the IntensityMapGraphicFloat due to the increased accuracy.
 * 
 */
//using SEL.API;
//using System;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.IO;

//namespace SEL
//{
//	class IntensityMapGraphicGDI: IntensityMapGraphic
//	{
//		private Bitmap m_outputBitmap;
//		private Graphics m_outputObject;

//		public IntensityMapGraphicGDI(int outputSizeX, int outputSizeY)
//			: base(outputSizeX, outputSizeY)
//		{
//			m_outputBitmap = new Bitmap(outputSizeX, outputSizeY);
//			m_outputObject = Graphics.FromImage(m_outputBitmap);
//			m_outputObject.Clear(Color.Black);
//		}

//		public override void Dispose()
//		{
//			base.Dispose();

//			m_outputObject.Dispose();
//			m_outputBitmap.Dispose();
//		}

//		public override void RenderLine(Point from, Point to, int intensityValue)
//		{
//			int alpha = (int)((float)intensityValue * 255.0f);
//			if (alpha <= 0)
//				return;

//			using (Pen edgePen = new Pen(Color.FromArgb(alpha, 255, 255, 255)))
//			{
//				m_outputObject.DrawLine(edgePen, from, to);
//			}
//		}

//		public override void SaveFile(string filePath, APIHeatmapSettings settings)
//		{
//			using (FileStream stream = new FileStream(filePath, FileMode.Create))
//			{
//				m_outputBitmap.Save(stream, ImageFormat.Png);
//			}
//		}
//	}
//}
