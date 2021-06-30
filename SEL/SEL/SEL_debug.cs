using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SEL
{
	class SEL_debug
	{
		private static RouteManager ms_routeManager = null;

		private struct DrawParameters
		{
			public double m_originX;
			public double m_originY;
			public double m_drawScale;
			public int m_graphicSize;

			public float TransformX(Vector2D pos)
			{
				return (float)((pos.x - m_originX) * m_drawScale);
			}

			public float TransformY(Vector2D pos)
			{
				return (float)m_graphicSize - (float)((pos.y - m_originY) * m_drawScale);
			}

			public PointF TransformPoint(Vector2D position)
			{
				return new PointF(TransformX(position), TransformY(position));
			}
		}

		public static void SetRouteManagerForDebugDraw(RouteManager routeManager)
		{
			ms_routeManager = routeManager;
		}

		public static void CreateEdgeMap(RouteManager routeManager, int dimensionsInPixels = 250)
		{
			Console.Write("Creating Edge Map...");
			DrawParameters parameters = CreateDrawParameters(routeManager.GetVertices(), dimensionsInPixels);

			using (Bitmap debugMap = new Bitmap(dimensionsInPixels, dimensionsInPixels))
			{
				using (Graphics graphic = Graphics.FromImage(debugMap))
				{
					graphic.Clear(Color.White);

					RenderLaneVertices(routeManager, graphic, parameters, Color.Red, true);
					RenderRestrictionEdges(routeManager, graphic, parameters);

					Pen persistentEdge = new Pen(Color.Blue, 1.0f);
					Pen implicitEdge = new Pen(Color.FromArgb(255, 0, 0, 0), 1.0f);
					foreach (LaneEdge edge in routeManager.GetEdges())
					{
						Pen edgePen = (edge.m_laneType == ELaneEdgeType.Implicit) ? implicitEdge : persistentEdge;
						Color edgeColor = Color.FromArgb(edgePen.Color.A, edgePen.Color.R, (int)(255.0f * edge.GetRestrictionOverlapAmount()), edgePen.Color.B);
						Pen drawPen = new Pen(edgeColor, 1.0f);

						graphic.DrawLine(drawPen, parameters.TransformX(edge.m_from.position), parameters.TransformY(edge.m_from.position), parameters.TransformX(edge.m_to.position), parameters.TransformY(edge.m_to.position));
					}
				}

				Directory.CreateDirectory("Output/");
				using (FileStream stream = new FileStream("Output/EdgeMap.png", FileMode.Create))
				{
					debugMap.Save(stream, ImageFormat.Png);
				}
			}

			Console.WriteLine("Done");
		}

		public static void CreateRouteMap(RouteManager routeManager, int dimensionsInPixels)
		{
			DrawParameters parameters = CreateDrawParameters(routeManager.GetVertices(), dimensionsInPixels);

			int routeCounter = 0;
			foreach (Route route in routeManager.GetAvailableRoutes())
			{
				Console.Write("Creating Route Map {0} / {1} \r", routeCounter, routeManager.GetAvailableRouteCount());
				++routeCounter;
				using (Bitmap debugMap = new Bitmap(dimensionsInPixels, dimensionsInPixels))
				{
					using (Graphics graphic = Graphics.FromImage(debugMap))
					{
						graphic.Clear(Color.White);
						RenderLaneVertices(routeManager, graphic, parameters, Color.Red, true);
						RenderRestrictionEdges(routeManager, graphic, parameters);
						Pen routePen = new Pen(Color.Magenta, 1.0f);
						foreach (LaneEdge edge in route.GetRouteEdges())
						{
							graphic.DrawLine(routePen, parameters.TransformX(edge.m_from.position), parameters.TransformY(edge.m_from.position), parameters.TransformX(edge.m_to.position), parameters.TransformY(edge.m_to.position));
						}

						Font shipTypeInfoFont = new Font(FontFamily.GenericSansSerif, 12);
						SolidBrush shipTypeInfoBrush = new SolidBrush(Color.Black);
						graphic.DrawString(route.ShipTypeInfo.GetDebugInfo(), shipTypeInfoFont, shipTypeInfoBrush, 4.0f, 4.0f);
					}

					Directory.CreateDirectory("Output/");
					using (FileStream stream = new FileStream("Output/RouteMap_from_" + route.FromVertex.vertexId + "_to_" + route.ToVertex.vertexId + ".png", FileMode.Create))
					{
						debugMap.Save(stream, ImageFormat.Png);
					}
				}
			}
			Console.WriteLine("Creating Route Map Done...");
		}

		public static void CreateRouteQueryDebugMap(LaneVertex from, LaneVertex to, List<LaneVertex> closedVertices, List<LaneEdge> closedEdges, int dimensionsInPixels)
		{
			DrawParameters parameters = CreateDrawParameters(ms_routeManager.GetVertices(), dimensionsInPixels);

			using (Bitmap debugMap = new Bitmap(dimensionsInPixels, dimensionsInPixels))
			{
				using (Graphics graphic = Graphics.FromImage(debugMap))
				{
					graphic.Clear(Color.White);
					RenderRestrictionEdges(ms_routeManager, graphic, parameters);
					RenderLaneVertices(ms_routeManager, graphic, parameters, Color.DodgerBlue, false);
					Pen routePen = new Pen(Color.Lime, 3.0f);
					Pen connectionPen = new Pen(Color.DarkGray, 1.0f);
					foreach (LaneVertex closedVertex in closedVertices)
					{
						float x = parameters.TransformX(closedVertex.position);
						float y = parameters.TransformY(closedVertex.position);
						float size = 5.0f;

						graphic.DrawEllipse(routePen, x - (size * 0.5f), y - (size * 0.5f), size, size);
						foreach (LaneEdge edge in closedVertex.GetConnections())
						{
							graphic.DrawLine(connectionPen, parameters.TransformX(edge.m_from.position), parameters.TransformY(edge.m_from.position), parameters.TransformX(edge.m_to.position), parameters.TransformY(edge.m_to.position));
						}
					}

					foreach (LaneEdge edge in closedEdges)
					{
						graphic.DrawLine(routePen, parameters.TransformX(edge.m_from.position), parameters.TransformY(edge.m_from.position), parameters.TransformX(edge.m_to.position), parameters.TransformY(edge.m_to.position));
					}

					Pen sourcePen = new Pen(Color.Fuchsia, 4.0f);
					graphic.DrawEllipse(sourcePen, parameters.TransformX(from.position), parameters.TransformY(from.position), 7.5f, 7.5f);
					Pen destinationPen = new Pen(Color.Black, 4.0f);
					graphic.DrawEllipse(destinationPen, parameters.TransformX(to.position), parameters.TransformY(to.position), 7.5f, 7.5f);
				}

				Directory.CreateDirectory("Output/");
				using (FileStream stream = new FileStream("Output/RouteFinder_from_" + from.vertexId + "_to_" + to.vertexId + ".png", FileMode.Create))
				{
					debugMap.Save(stream, ImageFormat.Png);
				}
			}
		}

		private static DrawParameters CreateDrawParameters(IEnumerable<LaneVertex> vertexCollection, int outputDimensionsInPixels)
		{
			double xMin = 1e25f;
			double yMin = 1e25f;
			double xMax = -1e25f;
			double yMax = -1e25f;

			DrawParameters result = new DrawParameters();

			foreach (LaneVertex vertex in vertexCollection)
			{
				if (vertex.position.x < xMin)
				{
					xMin = vertex.position.x;
				}
				if (vertex.position.y < yMin)
				{
					yMin = vertex.position.y;
				}

				if (vertex.position.x > xMax)
				{
					xMax = vertex.position.x;
				}
				if (vertex.position.y > yMax)
				{
					yMax = vertex.position.y;
				}
			}

			const int BORDER_SIZE = 4;
			double maxCoordinates = Math.Max(xMax - xMin, yMax - yMin);
			double rcpMaxCoordinates = (1.0 / maxCoordinates);
			result.m_drawScale = rcpMaxCoordinates * (float)(outputDimensionsInPixels - (BORDER_SIZE * 2));
			result.m_originX = xMin - (BORDER_SIZE / result.m_drawScale);
			result.m_originY = yMin - (BORDER_SIZE / result.m_drawScale);
			result.m_graphicSize = outputDimensionsInPixels;

			return result;
		}

		private static void RenderLaneVertices(RouteManager routeManager, Graphics graphic, DrawParameters parameters, Color vertexColor, bool displayNodeId)
		{
			Pen vertexPen = new Pen(vertexColor, 2.0f);
			Font vertexFont = new Font(FontFamily.GenericSansSerif, 12);
			SolidBrush vertexBrush = new SolidBrush(Color.Black);

			foreach (LaneVertex vertex in routeManager.GetVertices())
			{
				float x = parameters.TransformX(vertex.position);
				float y = parameters.TransformY(vertex.position);
				float size = 3.0f;
				graphic.DrawEllipse(vertexPen, x - (size * 0.5f), y - (size * 0.5f), size, size);
				if (displayNodeId)
				{
					graphic.DrawString(vertex.vertexId.ToString(), vertexFont, vertexBrush, x, y);
				}
			}
		}

		private static void RenderRestrictionEdges(RouteManager routeManager, Graphics graphic, DrawParameters parameters)
		{
			Pen restrictionPen = new Pen(Color.Red);
			foreach (RestrictionEdge edge in routeManager.GetRestrictionEdges())
			{
				graphic.DrawLine(restrictionPen, parameters.TransformPoint(edge.m_from.position), parameters.TransformPoint(edge.m_to.position));
			}
		}
	}
}
