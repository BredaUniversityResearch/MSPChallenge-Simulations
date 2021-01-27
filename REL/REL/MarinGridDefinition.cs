using System;
using System.Collections.Generic;
using System.IO;
using ProjNet.CoordinateSystems.Transformations;

namespace REL
{
	struct GeoCoordinate
	{
		public double Lat;
		public double Lon;

		public GeoCoordinate(double a_lat, double a_lon)
		{
			Lat = a_lat;
			Lon = a_lon;
		}
	};

	class MarinGridDefinition
	{
		private class MarinGridCsvData
		{
			public int GridX { get; set; }
			public int GridY { get; set; }
			public double Lat { get; set; }
			public double Lon { get; set; }
		};

		private GeoCoordinate[,] m_gridPositions;
		private Vector2D[,] m_mspProjectedGridPosition;

		private GeoCoordinate m_marinSpaceMinBounds = new GeoCoordinate(double.MaxValue, double.MaxValue);
		private GeoCoordinate m_marinSpaceMaxBounds = new GeoCoordinate(double.MinValue, double.MinValue);

		private Vector2D m_mspSpaceMinBounds = new Vector2D(double.MaxValue, double.MaxValue);
		private Vector2D m_mspSpaceMaxBounds = new Vector2D(double.MinValue, double.MinValue);

		public int GridXMin = int.MaxValue;
		public int GridXMax = int.MinValue;
		public int GridYMin = int.MaxValue;
		public int GridYMax = int.MinValue;

		public int GridWidth => (GridXMax - GridXMin) + 1;
		public int GridHeight => (GridYMax - GridYMin) + 1;

		private MarinGridDefinition(List<MarinGridCsvData> a_gridData, ICoordinateTransformation a_marinToMspTransformation)
		{
			foreach (MarinGridCsvData data in a_gridData)
			{
				m_marinSpaceMinBounds.Lat = Math.Min(m_marinSpaceMinBounds.Lat, data.Lat);
				m_marinSpaceMinBounds.Lon = Math.Min(m_marinSpaceMinBounds.Lon, data.Lon);
				m_marinSpaceMaxBounds.Lat = Math.Max(m_marinSpaceMaxBounds.Lat, data.Lat);
				m_marinSpaceMaxBounds.Lon = Math.Max(m_marinSpaceMaxBounds.Lon, data.Lon);

				GridXMin = Math.Min(GridXMin, data.GridX);
				GridXMax = Math.Max(GridXMax, data.GridX);
				GridYMin = Math.Min(GridYMin, data.GridY);
				GridYMax = Math.Max(GridYMax, data.GridY);
			}

			m_gridPositions = new GeoCoordinate[GridWidth, GridHeight];
			m_mspProjectedGridPosition = new Vector2D[GridWidth, GridHeight];

			foreach (MarinGridCsvData data in a_gridData)
			{
				m_gridPositions[data.GridX - GridXMin, data.GridY - GridYMin] = new GeoCoordinate(data.Lat, data.Lon);

				var projectedCoordinate = a_marinToMspTransformation.MathTransform.Transform(data.Lon, data.Lat);
				m_mspProjectedGridPosition[data.GridX - GridYMin, data.GridY - GridYMin] =
					new Vector2D(projectedCoordinate.x, projectedCoordinate.y);

				m_mspSpaceMinBounds.x = Math.Min(m_mspSpaceMinBounds.x, projectedCoordinate.x);
				m_mspSpaceMinBounds.y = Math.Min(m_mspSpaceMinBounds.y, projectedCoordinate.y);
				m_mspSpaceMaxBounds.x = Math.Max(m_mspSpaceMaxBounds.x, projectedCoordinate.x);
				m_mspSpaceMaxBounds.y = Math.Max(m_mspSpaceMaxBounds.y, projectedCoordinate.y);
			}
		}

		public Bounds2D GetMarinSpaceBounds()
		{
			return new Bounds2D(new Vector2D(m_marinSpaceMinBounds.Lat, m_marinSpaceMinBounds.Lon), new Vector2D(m_marinSpaceMaxBounds.Lat, m_marinSpaceMaxBounds.Lon));
		}

		public Bounds2D GetMspSpaceBounds()
		{
			return new Bounds2D(m_mspSpaceMinBounds, m_mspSpaceMaxBounds);
		}

		public static MarinGridDefinition LoadFromFile(string a_filePath, ICoordinateTransformation a_marinToMspTransformation)
		{
			MarinGridDefinition result = null;
			if (File.Exists(a_filePath))
			{
				using (TextReader fileReader = new StreamReader(a_filePath))
				{
					List<MarinGridCsvData> data = new List<MarinGridCsvData>(16384);
					int[] columnWidths = {8, 8, 11, 12};
					string line = fileReader.ReadLine();
					while (!string.IsNullOrEmpty(line))
					{
						int parsedCharacters = 0;
						MarinGridCsvData lineData = new MarinGridCsvData();
						for (int columnIndex = 0; columnIndex < columnWidths.Length; ++columnIndex)
						{
							string columnValue = line.Substring(parsedCharacters, columnWidths[columnIndex]);
							switch (columnIndex)
							{
								case 0:
									lineData.GridX = int.Parse(columnValue);
									break;
								case 1:
									lineData.GridY = int.Parse(columnValue);
									break;
								case 2:
									lineData.Lat = double.Parse(columnValue);
									break;
								case 3:
									lineData.Lon = double.Parse(columnValue);
									break;
								default:
									throw new ArgumentOutOfRangeException();
							}

							parsedCharacters += columnWidths[columnIndex];
						}

						data.Add(lineData);

						line = fileReader.ReadLine();
					}

					result = new MarinGridDefinition(data, a_marinToMspTransformation);
				}
			}

			return result;
		}

		public GeoCoordinate GetGridPositionMarinSpace(int a_x, int a_y)
		{
			return m_gridPositions[a_x - GridXMin, a_y - GridYMin];
		}

		public Vector2D GetGridPositionMspSpace(int a_x, int a_y)
		{
			return m_mspProjectedGridPosition[a_x - GridXMin, a_y - GridYMin];
		}
	}
}
