using System;
using System.Collections.Generic;
using System.IO;
using MSWSupport;
using SkiaSharp;

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

			public SKPoint TransformPoint(Vector2D position)
			{
				return new SKPoint(TransformX(position), TransformY(position));
			}
		}

		public static void SetRouteManagerForDebugDraw(RouteManager routeManager)
		{
			ms_routeManager = routeManager;
		}

		public static void CreateEdgeMap(RouteManager routeManager, int dimensionsInPixels = 250)
		{
			ConsoleLogger.Info("Creating Edge Map...");
			DrawParameters parameters = CreateDrawParameters(routeManager.GetVertices(), dimensionsInPixels);

			using (SKBitmap debugMap = new SKBitmap(dimensionsInPixels, dimensionsInPixels, SKColorType.Bgra8888, SKAlphaType.Premul))
			{
				using (SKCanvas graphic = new SKCanvas(debugMap))
				{
					graphic.Clear(SKColors.White);

					RenderLaneVertices(routeManager, graphic, parameters, SKColors.Red, true);
					RenderRestrictionEdges(routeManager, graphic, parameters);

					using SKPaint persistentEdge = new SKPaint { Color = SKColors.Blue, StrokeWidth = 1.0f, Style = SKPaintStyle.Stroke, IsAntialias = true };
					using SKPaint implicitEdge = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.0f, Style = SKPaintStyle.Stroke, IsAntialias = true };
					foreach (LaneEdge edge in routeManager.GetEdges())
					{
						SKPaint edgePaint = (edge.m_laneType == ELaneEdgeType.Implicit) ? implicitEdge : persistentEdge;
						byte restricted = (byte)Math.Clamp((int)(255.0f * edge.GetRestrictionOverlapAmount()), 0, 255);
						SKColor edgeColor = new SKColor(edgePaint.Color.Red, restricted, edgePaint.Color.Blue, edgePaint.Color.Alpha);
						using SKPaint drawPaint = new SKPaint { Color = edgeColor, StrokeWidth = 1.0f, Style = SKPaintStyle.Stroke, IsAntialias = true };

						graphic.DrawLine(parameters.TransformX(edge.m_from.position), parameters.TransformY(edge.m_from.position), parameters.TransformX(edge.m_to.position), parameters.TransformY(edge.m_to.position), drawPaint);
					}
				}

				Directory.CreateDirectory("Output/");
				SaveBitmap(debugMap, "Output/EdgeMap.png");
			}

			ConsoleLogger.Info("Finished Edge Map");
		}

		public static void CreateRouteMap(RouteManager routeManager, int dimensionsInPixels)
		{
			DrawParameters parameters = CreateDrawParameters(routeManager.GetVertices(), dimensionsInPixels);

			int routeCounter = 0;
			foreach (Route route in routeManager.GetAvailableRoutes())
			{
				ConsoleLogger.Info($"Creating Route Map {routeCounter} / {routeManager.GetAvailableRouteCount()}");
				++routeCounter;
				using (SKBitmap debugMap = new SKBitmap(dimensionsInPixels, dimensionsInPixels, SKColorType.Bgra8888, SKAlphaType.Premul))
				{
					using (SKCanvas graphic = new SKCanvas(debugMap))
					{
						graphic.Clear(SKColors.White);
						RenderLaneVertices(routeManager, graphic, parameters, SKColors.Red, true);
						RenderRestrictionEdges(routeManager, graphic, parameters);
						using SKPaint routePen = new SKPaint { Color = SKColors.Magenta, StrokeWidth = 1.0f, Style = SKPaintStyle.Stroke, IsAntialias = true };
						foreach (LaneEdge edge in route.GetRouteEdges())
						{
							graphic.DrawLine(parameters.TransformX(edge.m_from.position), parameters.TransformY(edge.m_from.position), parameters.TransformX(edge.m_to.position), parameters.TransformY(edge.m_to.position), routePen);
						}

						using SKPaint shipTypeInfoPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
						using SKFont shipTypeInfoFont = new SKFont(SKTypeface.Default, 12.0f);
						graphic.DrawText(route.ShipTypeInfo.GetDebugInfo(), 4.0f, 16.0f, SKTextAlign.Left, shipTypeInfoFont, shipTypeInfoPaint);
					}

					Directory.CreateDirectory("Output/");
					SaveBitmap(debugMap, "Output/RouteMap_from_" + route.FromVertex.vertexId + "_to_" + route.ToVertex.vertexId + ".png");
				}
			}
			ConsoleLogger.Info("Creating Route Map Done...");
		}

		public static void CreateRouteQueryDebugMap(LaneVertex from, LaneVertex to, List<LaneVertex> closedVertices, List<LaneEdge> closedEdges, int dimensionsInPixels)
		{
			DrawParameters parameters = CreateDrawParameters(ms_routeManager.GetVertices(), dimensionsInPixels);

			using (SKBitmap debugMap = new SKBitmap(dimensionsInPixels, dimensionsInPixels, SKColorType.Bgra8888, SKAlphaType.Premul))
			{
				using (SKCanvas graphic = new SKCanvas(debugMap))
				{
					graphic.Clear(SKColors.White);
					RenderRestrictionEdges(ms_routeManager, graphic, parameters);
					RenderLaneVertices(ms_routeManager, graphic, parameters, SKColors.DodgerBlue, false);
					using SKPaint routePen = new SKPaint { Color = SKColors.Lime, StrokeWidth = 3.0f, Style = SKPaintStyle.Stroke, IsAntialias = true };
					using SKPaint connectionPen = new SKPaint { Color = SKColors.DarkGray, StrokeWidth = 1.0f, Style = SKPaintStyle.Stroke, IsAntialias = true };
					foreach (LaneVertex closedVertex in closedVertices)
					{
						float x = parameters.TransformX(closedVertex.position);
						float y = parameters.TransformY(closedVertex.position);
						float size = 5.0f;

						graphic.DrawOval(SKRect.Create(x - (size * 0.5f), y - (size * 0.5f), size, size), routePen);
						foreach (LaneEdge edge in closedVertex.GetConnections())
						{
							graphic.DrawLine(parameters.TransformX(edge.m_from.position), parameters.TransformY(edge.m_from.position), parameters.TransformX(edge.m_to.position), parameters.TransformY(edge.m_to.position), connectionPen);
						}
					}

					foreach (LaneEdge edge in closedEdges)
					{
						graphic.DrawLine(parameters.TransformX(edge.m_from.position), parameters.TransformY(edge.m_from.position), parameters.TransformX(edge.m_to.position), parameters.TransformY(edge.m_to.position), routePen);
					}

					using SKPaint sourcePen = new SKPaint { Color = SKColors.Fuchsia, StrokeWidth = 4.0f, Style = SKPaintStyle.Stroke, IsAntialias = true };
					graphic.DrawOval(SKRect.Create(parameters.TransformX(from.position), parameters.TransformY(from.position), 7.5f, 7.5f), sourcePen);
					using SKPaint destinationPen = new SKPaint { Color = SKColors.Black, StrokeWidth = 4.0f, Style = SKPaintStyle.Stroke, IsAntialias = true };
					graphic.DrawOval(SKRect.Create(parameters.TransformX(to.position), parameters.TransformY(to.position), 7.5f, 7.5f), destinationPen);
				}

				Directory.CreateDirectory("Output/");
				SaveBitmap(debugMap, "Output/RouteFinder_from_" + from.vertexId + "_to_" + to.vertexId + ".png");
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

		private static void RenderLaneVertices(RouteManager routeManager, SKCanvas graphic, DrawParameters parameters, SKColor vertexColor, bool displayNodeId)
		{
			using SKPaint vertexPen = new SKPaint { Color = vertexColor, StrokeWidth = 2.0f, Style = SKPaintStyle.Stroke, IsAntialias = true };
			using SKPaint vertexTextPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
			using SKFont vertexFont = new SKFont(SKTypeface.Default, 12.0f);

			foreach (LaneVertex vertex in routeManager.GetVertices())
			{
				float x = parameters.TransformX(vertex.position);
				float y = parameters.TransformY(vertex.position);
				float size = 3.0f;
				graphic.DrawOval(SKRect.Create(x - (size * 0.5f), y - (size * 0.5f), size, size), vertexPen);
				if (displayNodeId)
				{
					graphic.DrawText(vertex.vertexId.ToString(), x, y, SKTextAlign.Left, vertexFont, vertexTextPaint);
				}
			}
		}

		private static void RenderRestrictionEdges(RouteManager routeManager, SKCanvas graphic, DrawParameters parameters)
		{
			using SKPaint restrictionPen = new SKPaint { Color = SKColors.Red, StrokeWidth = 1.0f, Style = SKPaintStyle.Stroke, IsAntialias = true };
			foreach (RestrictionEdge edge in routeManager.GetRestrictionEdges())
			{
				graphic.DrawLine(parameters.TransformPoint(edge.m_from.position), parameters.TransformPoint(edge.m_to.position), restrictionPen);
			}
		}

		private static void SaveBitmap(SKBitmap bitmap, string filePath)
		{
			using SKImage image = SKImage.FromBitmap(bitmap);
			using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
			using FileStream stream = new FileStream(filePath, FileMode.Create);
			data.SaveTo(stream);
		}
	}
}
