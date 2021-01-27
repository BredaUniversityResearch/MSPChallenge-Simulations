using System;
using System.Runtime.Serialization;
using SEL.SpatialMapping;

namespace SEL.API
{
	internal class APIAreaOutputConfiguration
	{
		public APIBounds simulation_area;
		public int pixels_per_mel_cell;	//Resolution multiplier based off MEL's cell size. With this we can derive the output resolution (melCellSize / pixels_per_mel_cell) / [areaWidth|areaHeight]
		public double simulation_area_cell_size = 0.0f; //Cell size when mel is absent.

		public APIBounds mel_area;
		public int mel_cell_size;
		public int mel_resolution_x;
		public int mel_resolution_y;

		public AABB GetAlignedSimulationBounds()
		{
			if (mel_area != null)
			{
				double xOffset = simulation_area.x_min - mel_area.x_min;
				//double yOffset = simulation_area.y_max - mel_area.y_max; //MEL defines the top-left as x_min, y_max so we align on y_max as well.
				double yOffset = simulation_area.y_min - mel_area.y_min;

				Vector2D simulationShift = new Vector2D(xOffset % mel_cell_size, yOffset % mel_cell_size);

				if (Math.Abs(simulationShift.x) > 0.01 || Math.Abs(simulationShift.y) > 0.01)
				{
					ErrorReporter.ReportError(EErrorSeverity.Error, "Receved unaligned simulation bounds. Simulation bounds should be aligned with the MEL bounds.");
				}
			}

			return simulation_area.ToAABB();
		}

		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			if (mel_area != null)
			{
				if (pixels_per_mel_cell == 0)
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						"Output area configuration does not specify the pixels_per_mel_cell value. Make sure this is configured when using SEL in cooperation with MEL. Defaulting this value to 1");
					pixels_per_mel_cell = 1;
				}
			}
			else
			{
				if (simulation_area_cell_size <= 0.0)
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						"Output area configuration does not specify a value simulation_area_cell_size value. This value should be specified when MEL is not configured to a value larger than 0. Defaulting this value to 1000");
					simulation_area_cell_size = 1000.0;
				}
			}
		}
	}
}