using System;
using System.Collections.Generic;
using System.IO;
using ProjNet.CoordinateSystems.Transformations;
using REL.API;

namespace REL
{
	class GridHeatmapRenderer
	{
		private MarinGridDefinition m_marinGridDefinition;

		private HeatmapCoordinateConverter m_mspToHeatmap;

		public GridHeatmapRenderer(MarinGridDefinition a_gridDefinition)
		{
			m_marinGridDefinition = a_gridDefinition;

			m_mspToHeatmap = new HeatmapCoordinateConverter(m_marinGridDefinition);
		}

		public HeatmapDataGrid CreateHeatmap()
		{
			return new HeatmapDataGrid(m_mspToHeatmap.OutputPixelWidth, m_mspToHeatmap.OutputPixelHeight);
		}

		public void RenderRestrictionData(MarinAPIResponseGeometryData[] a_responseRestrictionContacts, Dictionary<uint, MSPAPIGeometry> a_mspGeometryByName, HeatmapDataGrid a_targetHeatMap)
		{
			//Vector2D topLeftGrid = m_mspToHeatmap.ToRasterPosition(m_mspToHeatmap.OutputAreaTopLeftMspSpace);
			//Vector2D topRightGrid = m_mspToHeatmap.ToRasterPosition(m_mspToHeatmap.OutputAreaTopRightMspSpace);
			//Vector2D bottomLeftGrid = m_mspToHeatmap.ToRasterPosition(m_mspToHeatmap.OutputAreaBottomLeftMspSpace);
			//Vector2D bottomRightGrid = m_mspToHeatmap.ToRasterPosition(m_mspToHeatmap.OutputAreaBottomRightMspSpace);

			foreach (MarinAPIResponseGeometryData data in a_responseRestrictionContacts)
			{
				if (a_mspGeometryByName.TryGetValue(data.geometry_id, out MSPAPIGeometry geometry))
				{
					Vector2D from = m_mspToHeatmap.ToRasterPosition(geometry.geometry[data.segment_id]);
					Vector2D to = m_mspToHeatmap.ToRasterPosition(geometry.geometry[data.segment_id + 1]);
					a_targetHeatMap.PlotLine(from, to, (float)data.contacts);
				}
				else
				{
					Console.WriteLine(
						$"Failed to find geometry with id {data.geometry_id} from Marin response in original data set.");
				}
			}
		}

		public void RenderShipCollisionData(MarinAPIResponseShipCollisionData[] a_responseShipCollisionData,
			HeatmapDataGrid a_targetGrid)
		{
			foreach (MarinAPIResponseShipCollisionData shipCollision in a_responseShipCollisionData)
			{
				Vector2D mspSpaceCenterPoint = m_marinGridDefinition.GetGridPositionMspSpace(shipCollision.x, shipCollision.y);
				Vector2D gridPoint = m_mspToHeatmap.ToRasterPosition(mspSpaceCenterPoint);
				a_targetGrid.AdditivePlot((float)gridPoint.x, (float)gridPoint.y, (float)shipCollision.collisions);
			}
		}
	}
}
