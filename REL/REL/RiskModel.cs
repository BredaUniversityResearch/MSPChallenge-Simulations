using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MSWSupport;
using Newtonsoft.Json;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using REL.API;
using SELRELBridge;
using SELRELBridge.API;

namespace REL
{
	class RiskModel
	{
		private APITokenHandler m_tokenHandler;
		private IMSPAPIConnector m_mspApiConnector = new MSPAPIExternalServer();
		private IMarinAPIConnector m_marinApiConnector = new MarinAPIConnectorDebug();

		private MSPAPIRELConfig m_relServerConfig = null;

		private BridgeClient m_selBridge;

		private readonly CoordinateSystem m_MSPCoordinateSystem;
		private readonly CoordinateSystem m_MarinCoordinateSystem;
		private readonly ICoordinateTransformation m_MSPToMarinTransformation;
		private readonly ICoordinateTransformation m_MarinToMSPTransformation;

		public RiskModel()
		{
			string pipeHandle = null;
			if (CommandLineArguments.HasOptionValue(MSWConstants.MSWPipeCommandLineArgument))
			{
				pipeHandle = CommandLineArguments.GetOptionValue(MSWConstants.MSWPipeCommandLineArgument);
			}

			string watchdogToken = WatchdogTokenUtility.GetWatchdogTokenForServerAtAddress(RELConfig.Instance.GetAPIRoot());
			m_selBridge = new BridgeClient(watchdogToken);

			m_tokenHandler = new APITokenHandler(m_mspApiConnector, pipeHandle, "REL", RELConfig.Instance.GetAPIRoot());

			CoordinateSystemFactory coordinateSystemFactory = new CoordinateSystemFactory();
			CoordinateTransformationFactory transformationFactory = new CoordinateTransformationFactory();
			m_MSPCoordinateSystem = coordinateSystemFactory.CreateFromWkt(CoordinateSystemWKT.EPSG_3035);
			m_MarinCoordinateSystem = coordinateSystemFactory.CreateFromWkt(CoordinateSystemWKT.WGS84);
			m_MSPToMarinTransformation = transformationFactory.CreateFromCoordinateSystems(m_MSPCoordinateSystem, m_MarinCoordinateSystem);
			m_MarinToMSPTransformation = transformationFactory.CreateFromCoordinateSystems(m_MarinCoordinateSystem, m_MSPCoordinateSystem);
		}

		public void Run()
		{
			m_relServerConfig = m_mspApiConnector.GetConfiguration();

			while (true)
			{
				if (m_tokenHandler.CheckApiAccessWithLatestReceivedToken())
				{
					m_selBridge.PumpReceivedMessages(ProcessReceivedMessages);
				}
				else
				{
					Console.WriteLine("API refused access... Waiting for a bit and retrying");
					Thread.Sleep(2500);
				}

				Thread.Sleep(100);
			}

			//PerformUpdate();
		}

		private void ProcessReceivedMessages(int a_messageType, string a_messageData)
		{
			if (a_messageType == SELOutputData.MessageIdentifier)
			{
				SELOutputData data = JsonConvert.DeserializeObject<SELOutputData>(a_messageData);
				Console.WriteLine($"REL\t|Received input data for month {data.m_simulatedMonth}. Processing...");
				PerformUpdate(data);


				//Debug
				Console.WriteLine($"REL\t|Using dummy response from marin api ...");
				MarinAPIProcessResponse response = m_marinApiConnector.TryGetProcessResponse();
				if (response != null)
				{
					MSPAPIGeometry[] geom = m_mspApiConnector.GetGeometry();
					Dictionary<uint, MSPAPIGeometry> mspRestrictionGeometryById = new Dictionary<uint, MSPAPIGeometry>(geom.Length);
					foreach (MSPAPIGeometry geometry in geom)
					{
						mspRestrictionGeometryById.Add(geometry.geometry_id, geometry);
					}

					ProcessMarinResponse(response, mspRestrictionGeometryById);
				}
			}
		}

		private void PerformUpdate(SELOutputData a_inputData)
		{
			MSPAPIGeometry[] geometry = m_mspApiConnector.GetGeometry();
			MSPAPIDate date = m_mspApiConnector.GetDateForSimulatedMonth(a_inputData.m_simulatedMonth);

			MarinAPIPoint[] marinPoints = TransformPoints(a_inputData.m_routeGraphPoints);
			MarinAPILink[] marinEdges = TransformEdges(a_inputData.m_routeGraphEdges);
			MarinAPITraffic[] marinTraffic = TransformIntensities(a_inputData.m_routeGraphIntensities);
			MarinAPIGeometry[] marinGeometry = TransformGeometry(geometry);
			MarinAPIInput input = new MarinAPIInput
			{
				date = new MarinAPIDate(date.month_of_year, date.year),
				points = marinPoints,
				links = marinEdges,
				traffic = marinTraffic,
				restriction_data = marinGeometry
			};

			m_marinApiConnector.SubmitInput(input);

			Console.WriteLine($"REL\t|Submitted input data to Marin API... Processing the response is TODO at this point...");
		}

		private MarinAPIPoint[] TransformPoints(APIRouteGraphVertex[] a_points)
		{
			MarinAPIPoint[] result = new MarinAPIPoint[a_points.Length];
			for (int i = 0; i < a_points.Length; ++i)
			{
				double[] mspPosition = { a_points[i].position_x, a_points[i].position_y};
				double[] marinPosition = m_MSPToMarinTransformation.MathTransform.Transform(mspPosition);
				result[i] = new MarinAPIPoint{ point_id = (ushort)a_points[i].vertex_id, lat = marinPosition[1], lon = marinPosition[0]};
			}

			return result;
		}

		private MarinAPILink[] TransformEdges(APIRouteGraphEdge[] a_edges)
		{
			MarinAPILink[] result = new MarinAPILink[a_edges.Length];

			for (int i = 0; i < a_edges.Length; ++i)
			{
				result[i] = new MarinAPILink
				{
					link_id = (ushort)a_edges[i].edge_id,
					point_id_start = (ushort)a_edges[i].from_vertex_id,
					point_id_end = (ushort)a_edges[i].to_vertex_id,
					link_width = a_edges[i].edge_width
				};
			}

			return result;
		}

		private MarinAPITraffic[] TransformIntensities(APIRouteGraphEdgeIntensity[] a_intensities)
		{
			MarinAPITraffic[] result = new MarinAPITraffic[a_intensities.Length];
			for (int i = 0; i < a_intensities.Length; ++i)
			{
				result[i] = new MarinAPITraffic 
				{
					link_id = (ushort)a_intensities[i].edge_id,
					ship_type = a_intensities[i].ship_type_id,
					intensity = a_intensities[i].intensity
				};
			}

			return result;
		}

		private MarinAPIGeometry[] TransformGeometry(MSPAPIGeometry[] a_geometry)
		{
			MarinAPIGeometry[] result = new MarinAPIGeometry[a_geometry.Length];
			for (int i = 0; i < a_geometry.Length; ++i)
			{
				result[i] = new MarinAPIGeometry
				{
					geometry_id = a_geometry[i].geometry_id,
					geometry_type = a_geometry[i].geometry_type,
					geometry = TransformGeometryData(OrderMSPGeometryDataCounterClockwise(a_geometry[i]))
				};
			}

			return result;
		}

		private Vector2D[] OrderMSPGeometryDataCounterClockwise(MSPAPIGeometry a_geometry)
		{
			Vector2D centerPoint = new Vector2D(0.0, 0.0);
			for (int i = 0; i < a_geometry.geometry.Length; ++i)
			{
				centerPoint += a_geometry.geometry[i];
			}
			centerPoint *= (1.0 / a_geometry.geometry.Length);

			int order = 0;
			for (int i = 1; i < a_geometry.geometry.Length; ++i)
			{
				Vector2D fromPoint = a_geometry.geometry[i - 1] - centerPoint;
				Vector2D toPoint = a_geometry.geometry[i] - centerPoint;

				double det = fromPoint.CrossProduct(toPoint);
				order += (det < 0.0? -1 : 1);
			}

			Vector2D[] result = new Vector2D[a_geometry.geometry.Length];
			Array.Copy(a_geometry.geometry, result, a_geometry.geometry.Length);
			if (order < 0.0)
			{
				Array.Reverse(result);
			}

			return result;
		}

		private Vector2D[] TransformGeometryData(Vector2D[] a_geometry)
		{
			Vector2D[] result = new Vector2D[a_geometry.Length];
			for (int i = 0; i < a_geometry.Length; ++i)
			{
				var transformed = m_MSPToMarinTransformation.MathTransform.Transform(a_geometry[i].x, a_geometry[i].y);
				result[i] = new Vector2D(transformed.y, transformed.x);
			}

			return result;
		}

		private void ProcessMarinResponse(MarinAPIProcessResponse a_response, Dictionary<uint, MSPAPIGeometry> a_mspGeometryByName)
		{
			MarinGridDefinition gridDefinition = MarinGridDefinition.LoadFromFile("grid_digitwin_250x300_5000.txt", m_MarinToMSPTransformation);

			GridHeatmapRenderer renderer = new GridHeatmapRenderer(gridDefinition);
			HeatmapDataGrid contactsHeatmap = renderer.CreateHeatmap();
			renderer.RenderRestrictionData(a_response.restriction_contacts, a_mspGeometryByName, contactsHeatmap);

			using (MemoryStream ms = new MemoryStream(16384))
			{
				contactsHeatmap.WriteImageAsPngToStream(ms);

				m_mspApiConnector.UpdateRaster(m_relServerConfig.contacts_output_layer, gridDefinition.GetMspSpaceBounds(), ms.ToArray());
			}

			HeatmapDataGrid collisionDataGrid = renderer.CreateHeatmap();
			renderer.RenderShipCollisionData(a_response.ship_collision_data, collisionDataGrid);

			using (MemoryStream ms = new MemoryStream(16384))
			{
				collisionDataGrid.WriteImageAsPngToStream(ms);

				m_mspApiConnector.UpdateRaster(m_relServerConfig.collision_output_layer, gridDefinition.GetMspSpaceBounds(), ms.ToArray());
			}
		}
	}
}
