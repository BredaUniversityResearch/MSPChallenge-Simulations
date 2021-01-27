using SEL.API;
using SEL.KPI;
using SEL.Issues;
using SEL.Util;
using SELRELBridge;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MSWSupport;

namespace SEL
{
	class ShippingModel
	{
		private IApiConnector m_apiConnector = new APIConnectorExternalServer();
		private APITokenHandler m_apiTokenHandler = null;
		private ErrorReporter m_errorReporter = null;
		private ShippingPortManager m_portManager;
		private RouteManager m_routeManager;
		private ShipTypeManager m_shipTypeManager = new ShipTypeManager();
		private RestrictionGeometryTypeManager m_restrictionExceptionManager = new RestrictionGeometryTypeManager();
		private RouteIntensityManager m_routeIntensityManager = new RouteIntensityManager();
		private KPIManager m_KPIManager;
		private ShippingIssueManager m_shippingIssueManager = new ShippingIssueManager();
		private RELSupport m_relSupport;

		private APIShippingPortIntensity[] m_shippingPortIntensity = null;
		private APISELRegionSettings m_regionSpecificSettings = null;
		private APIAreaOutputConfiguration m_outputAreaConfiguration = null;
		private RasterOutputManager m_rasterOutputManager = new RasterOutputManager();
		private int m_lastUpdatedMonthId = -2;

		private bool m_hasLoadedInitialData;
		private bool m_hadApiAccessLastUpdate = false;

		public ShippingModel()
		{
			string pipeHandle = null;
			if (CommandLineArguments.HasOptionValue(CommandLineArguments.MSWPipe))
			{
				pipeHandle = CommandLineArguments.GetOptionValue(CommandLineArguments.MSWPipe);
			}

			string watchdogToken = WatchdogTokenUtility.GetWatchdogTokenForServerAtAddress(SELConfig.Instance.GetAPIRoot());
			Console.WriteLine($"Targeting simulation for server at {SELConfig.Instance.GetAPIRoot()} with token {watchdogToken}");

			if (!SELConfig.Instance.ShouldIgnoreApiSecurity())
			{
				m_apiTokenHandler = new APITokenHandler(m_apiConnector, pipeHandle, "SEL", SELConfig.Instance.GetAPIRoot());
			}

			m_relSupport = new RELSupport(watchdogToken);
			m_errorReporter = new ErrorReporter(m_apiConnector);
			m_KPIManager = new KPIManager(m_apiConnector);
		}

		private void LoadConfigurationData()
		{
			Task regionSettings = Task.Run(() =>
			{
				m_regionSpecificSettings = m_apiConnector.GetSELRegionSettings();
			});

			Task.WaitAll(regionSettings);
		}

		public void LoadPersistentConfigData()
		{
			APIShipType[] shipTypeInfo = null;
			APIRestrictionTypeException[] restrictionGeometryTypes = null;

			Task shipTypeTask = Task.Run(() =>
			{
				shipTypeInfo = m_apiConnector.GetShipTypes();
			});

			Task restrictionGeometryTypesTask = Task.Run(() =>
			{
				restrictionGeometryTypes = m_apiConnector.GetRestrictionTypeExceptions();
			});

			Task.WaitAll(shipTypeTask, restrictionGeometryTypesTask);

			m_shipTypeManager.ImportShipTypes(shipTypeInfo);
			m_restrictionExceptionManager.ImportGeometryTypes(restrictionGeometryTypes);
		}

		public void LoadSharedMapData()
		{
			APIHeatmapOutputSettings[] heatmapOutputSettings = null;
			APIHeatmapSettings riskmapSettings = null;
			APIConfiguredIntensityRoute[] configuredRouteIntensities = null;

			Task playableAreaTask = Task.Run(() =>
			{
				m_outputAreaConfiguration = m_apiConnector.GetAreaOutputConfiguration();
			});

			Task portTask = Task.Run(() =>
			{
				m_shippingPortIntensity = m_apiConnector.GetShippingPortIntensity();
			});

			Task configuredRouteTask = Task.Run(() =>
			{
				configuredRouteIntensities = m_apiConnector.GetConfiguredRouteIntensities();
			});

			Task heatmapDataTask = Task.Run(() =>
			{
				heatmapOutputSettings = m_apiConnector.GetHeatmapOutputSettings();
			});

			Task riskmapDataTask = Task.Run(() =>
			{
				riskmapSettings = m_apiConnector.GetHeatmapSettings();
			});

			Task.WaitAll(playableAreaTask, portTask, configuredRouteTask, heatmapDataTask, riskmapDataTask);

			m_rasterOutputManager.SetOutputConfiguration(m_outputAreaConfiguration);
			m_rasterOutputManager.ImportSharedHeatmapSettings(riskmapSettings);
			m_rasterOutputManager.ImportRasterData(heatmapOutputSettings, m_shipTypeManager);
			m_routeIntensityManager.ImportConfiguredRoutes(configuredRouteIntensities);
		}

		public void LoadRouteManagerData(RouteManager routeManager, ShippingPortManager portManager)
		{
			APIShippingLaneGeometry[] shippingLaneGeometry = null;
			APIShippingRestrictionGeometry[] restrictionGeometry = null;
			APIShippingPortGeometry[] shippingPortGeometry = null;

			Task laneTask = Task.Run(() =>
			{
				shippingLaneGeometry = m_apiConnector.GetShippingLanes();
			});

			Task restrictionTask = Task.Run(() =>
			{
				restrictionGeometry = m_apiConnector.GetRestrictionGeometry();
			});

			Task portTask = Task.Run(() =>
			{
				shippingPortGeometry = m_apiConnector.GetShippingPortGeometry();
			});

			using (new PerformanceTimer("API RouteManager Receive & deserialise"))
			{
				Task.WaitAll(laneTask, restrictionTask, portTask);
			}

			SEL_debug.SetRouteManagerForDebugDraw(routeManager);

			routeManager.SetSimulationArea(m_outputAreaConfiguration.GetAlignedSimulationBounds());

			using (new PerformanceTimer("ImportLanes"))
			{
				routeManager.ImportLanes(shippingLaneGeometry, m_shipTypeManager, m_regionSpecificSettings);
			}

			using (new PerformanceTimer("Import Ports"))
			{
				portManager.ImportPorts(shippingPortGeometry, routeManager);
			}

			using (new PerformanceTimer("Update Intensities"))
			{
				portManager.ImportIntensityData(m_shippingPortIntensity, m_regionSpecificSettings.maintenance_destinations, portManager, m_shipTypeManager);
			}

			using (new PerformanceTimer("Import Restriction"))
			{
				routeManager.ImportRestrictions(restrictionGeometry, m_restrictionExceptionManager, m_regionSpecificSettings.restriction_point_size);
			}

			using (new PerformanceTimer("Rasterizing Restriction Meshes"))
			{
				routeManager.RasterizeRestrictionMeshes(m_rasterOutputManager.GetRasterizerBoundRect(), m_rasterOutputManager.GetFullOutputResolution());
			}

			using (new PerformanceTimer("Rebuild Implicit Edges"))
			{
				routeManager.RebuildImplicitEdges(m_regionSpecificSettings.shipping_lane_implicit_distance_limit, portManager, m_regionSpecificSettings.port_lane_max_merge_distance);
			}
		}

		public void Tick()
		{
			if (SELConfig.Instance.ShouldIgnoreApiSecurity() || m_apiConnector.CheckApiAccess())
			{
				m_hadApiAccessLastUpdate = true;
				if (!m_hasLoadedInitialData)
				{
					using (new PerformanceTimer("Load Persistent Data"))
					{
						LoadConfigurationData();
						LoadPersistentConfigData();
						LoadSharedMapData();
					}
					Console.WriteLine("\t| Done. Awaiting game updates...");

					m_hasLoadedInitialData = true;
				}

				int monthId = m_apiConnector.GetCurrentMonth();
				if (monthId != m_lastUpdatedMonthId)
				{
					APIUpdateDescriptor updateDescriptor = m_apiConnector.GetUpdatePackage();
					if (m_routeManager == null || updateDescriptor.rebuild_edges)
					{
						m_shippingIssueManager.ClearIssues();
						m_routeManager = new RouteManager(m_shippingIssueManager);
						m_portManager = new ShippingPortManager();
						LoadRouteManagerData(m_routeManager, m_portManager);
					}

					ExecuteUpdate(monthId, m_routeManager, m_portManager);
					m_lastUpdatedMonthId = monthId;

					m_apiConnector.SetUpdateFinished(m_lastUpdatedMonthId);

					if (SELConfig.Instance.ShouldCreateEdgeMap())
					{
						SEL_debug.CreateEdgeMap(m_routeManager, SELConfig.Instance.EdgeMapOutputResolution());
					}

					if (SELConfig.Instance.ShouldCreateRouteMaps())
					{
						SEL_debug.CreateRouteMap(m_routeManager, SELConfig.Instance.RouteMapOutputResolution());
					}
				}
			}
			else
			{
				if (m_hadApiAccessLastUpdate)
				{
					Console.WriteLine("API token is not set or invalid... Waiting for new token...");
				}

				m_hadApiAccessLastUpdate = false;
			}
		}

		private void ExecuteUpdate(int timeMonth, RouteManager routeManager, ShippingPortManager portManager)
		{
			using (new PerformanceTimer(string.Format("Update Port Intensities for Month {0}", timeMonth)))
			{
				portManager.UpdatePortIntensities(timeMonth, portManager, m_shipTypeManager);
			}

			using (new PerformanceTimer(string.Format("Rebuild routing table for Month {0}", timeMonth)))
			{
				m_routeIntensityManager.RebuildRoutingEntries(m_shipTypeManager, routeManager, portManager, timeMonth);
			}

			using (new PerformanceTimer(string.Format("Update Routes for Month {0}", timeMonth)))
			{
				routeManager.UpdateRoutes(m_routeIntensityManager, m_shipTypeManager);
			}

			using (new PerformanceTimer("Restrictions Map Rebuild"))
			{
				m_rasterOutputManager.UpdateRestrictionMaps(routeManager, m_shipTypeManager);
			}	

			using (new PerformanceTimer(string.Format("Build Output Raster for Month {0}", timeMonth)))
			{
				BuildOutputRasters(timeMonth);
			}

			using (new PerformanceTimer(string.Format("Calculate + Submit KPIs for Month {0}", timeMonth)))
			{
				KPIInputData data = new KPIInputData(timeMonth, m_portManager, m_routeIntensityManager, routeManager, m_rasterOutputManager);
				m_KPIManager.CalculateKPIs(data);
				m_KPIManager.SubmitKPIResults(m_apiConnector);
			}

			using (new PerformanceTimer(string.Format("Submit shipping issues for Month {0}", timeMonth)))
			{
				m_shippingIssueManager.SubmitPendingIssues(m_apiConnector);
			}

			using (new PerformanceTimer($"Updating generated route graph and intensity for Month {timeMonth}"))
			{
				m_relSupport.SubmitResults(m_routeManager, m_routeIntensityManager, timeMonth, m_portManager);
			}

			Console.WriteLine("\t| Finished Update for Month {0}", timeMonth);
		}

		private void BuildOutputRasters(int timeMonth)
		{
			RouteIntensity[] allRouteIntensities = m_routeIntensityManager.GetAllAbsoluteRouteIntensities(timeMonth, m_portManager);
			m_rasterOutputManager.UpdateOutputRasters(allRouteIntensities, m_shipTypeManager, m_routeManager, m_apiConnector);
		}
	}
}
