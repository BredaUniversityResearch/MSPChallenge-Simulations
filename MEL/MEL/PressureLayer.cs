using System;
using System.Collections.Generic;
using EwEShell;
using System.Drawing;

namespace MEL
{
	/// <summary>
	/// A pressure layer is a layer that corresponds to a pressure in EwE.
	/// The layers are gathered from MSP, overlayed on top of each other with a certain multiplier or 'influence'
	/// The flattened, and rasterized, result is passed to EwE for processing.
	/// </summary>
	public class PressureLayer
	{
		public class LayerEntry
		{
			public readonly float influence;
			public readonly RasterizedLayer RasterizedLayer;

			public LayerEntry(RasterizedLayer rasterizedLayer, float influence)
			{
				this.influence = influence;
				this.RasterizedLayer = rasterizedLayer;
			}
		}

		private readonly string name;

		private List<LayerEntry> layers = new List<LayerEntry>();
		public bool redraw = true;
		public cPressure pressure;
		public double[,] rawData { get; private set; }

		public PressureLayer(string name)
		{
			this.name = name;
		}

		/// <summary>
		/// add a layer to the pressure
		/// </summary>
		public void Add(RasterizedLayer rasterizedLayer, float influence)
		{
			if (!rasterizedLayer.IsLoadedCorrectly)
			{
				Console.WriteLine(
					$"Tried to add layer {rasterizedLayer.name} to pressure. But this layer failed to load correctly");
				return;
			}

			layers.Add(new LayerEntry(rasterizedLayer, influence));
		}

		public void RasterizeLayers(MEL mel)
		{
			if (redraw)
			{
				//double total = 0f;

				rawData = new double[MEL.x_res, MEL.y_res];
				Console.WriteLine("rasterizing " + name);
				redraw = false;

				if (mel.ApiConnector.ShouldRasterizeLayers)
				{
					try
					{
						foreach (LayerEntry layerEntry in layers)
						{
							if (layerEntry == null)
							{
								Console.WriteLine("null layer");
								continue;
							}

							if (!layerEntry.RasterizedLayer.IsLoadedCorrectly)
							{
								Console.WriteLine(
									$"Tried to rasterize {layerEntry.RasterizedLayer.name}, but the layer failed to load correctly");
								continue;
							}

							for (int i = 0; i < MEL.x_res; i++)
							{
								for (int j = 0; j < MEL.y_res; j++)
								{
									rawData[i, j] += (layerEntry.RasterizedLayer.rawData[i, j] * layerEntry.influence);

									if (rawData[i, j] > 1)
									{
										rawData[i, j] = 1;
									}

									//total += this.rawdata[i, j];
								}
							}
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
				else
				{
					rawData = mel.ApiConnector.GetRasterizedPressure(name);
				}

				//Console.WriteLine(this.name + " : " + total.ToString());

				//set the data to be sent to EwE
				pressure = new cPressure(name, MEL.x_res, MEL.y_res, rawData);

				using (Bitmap bitmap = Rasterizer.ToBitmapSlow(rawData))
				{
					mel.ApiConnector.SubmitRasterLayerData(name, bitmap);
				}
			}
		}

		public IEnumerable<LayerEntry> GetLayerEntries()
		{
			return layers;
		}
	}
}
