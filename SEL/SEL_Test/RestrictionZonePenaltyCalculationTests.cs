using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEL;
using SEL.API;
using SEL.Issues;
using SEL.RasterizerLib;
using SEL.Routing;
using SEL.SpatialMapping;

namespace SEL_Test
{
	[TestClass]
	public class RestrictionZonePenaltyCalculationTests
	{
		private RestrictionGeometryTypeManager m_typeManager;
		private ShipTypeManager m_shipTypeManager = null;
		private APISELRegionSettings m_regionSettings = new APISELRegionSettings { };
		private static readonly AABB m_simulationArea = new AABB(new Vector2D(-10000.0, -10000.0), new Vector2D(10000.0, 10000.0));
		private static readonly Rect m_rasterizationRect = new Rect((float)m_simulationArea.min.x, (float)m_simulationArea.min.y,
			(float)m_simulationArea.max.x, (float)m_simulationArea.max.y);

		[TestInitialize]
		public void SetupTests()
		{
			APIRestrictionTypeException[] typeExceptions = new[]
			{
				new APIRestrictionTypeException{allowed_ship_type_ids = new [] { 1 } , cost_multipliers = new [] { 100.0f }, layer_id = 1, layer_type_id = 1}
			};
			m_typeManager = new RestrictionGeometryTypeManager();
			m_typeManager.ImportGeometryTypes(typeExceptions);

			m_shipTypeManager = new ShipTypeManager();

			APIShipType[] shipTypes = new[]
			{
				new APIShipType{ship_type_id = 1, ship_type_name = "TestShip", ship_agility = 1.0f, ship_restriction_group = "Test", ship_routing_type = EShipRoutingType.RegularShipping}
			};
			m_shipTypeManager.ImportShipTypes(shipTypes);
		}

		[TestMethod]
		public void NoIntersection()
		{
			ShippingIssueManager issueManager = new ShippingIssueManager();
			RouteManager routeManager = new RouteManager(issueManager);
			APIShippingRestrictionGeometry[] restrictionGeometry =
			{
				new APIShippingRestrictionGeometry
				{
					geometry = new[] {new[] {500.0, 500.0}, new[] {1000.0, 500.0}, new[] {1000.0, 1000.0}, new[] {500.0, 1000.0}},
					geometry_id = 1, layer_id = 1, layer_types = new[] {1}
				}
			};

			APIShippingLaneGeometry[] laneGeometry = new[]
			{
				new APIShippingLaneGeometry{geometry = new []{new []{400.0, 400.0}}, geometry_id = 2, ship_type_ids = new []{ 1 }},
				new APIShippingLaneGeometry{geometry = new []{new []{400.0, 1100.0}}, geometry_id = 2, ship_type_ids = new []{ 1 }}
			};

			routeManager.SetSimulationArea(m_simulationArea);
			routeManager.ImportRestrictions(restrictionGeometry, m_typeManager, 5000);
			routeManager.ImportLanes(laneGeometry, m_shipTypeManager, m_regionSettings);
			ShippingPortManager portManager = new ShippingPortManager();

			routeManager.RebuildImplicitEdges(1000, portManager, 0.1);

			LaneEdge edge = routeManager.GetEdgeByIndex(0);
			Assert.IsTrue(edge.m_laneType == ELaneEdgeType.Implicit, "edge.m_laneType == ELaneEdgeType.Implicit");
			Assert.IsTrue(edge.IsShipTypeAllowed(1), "edge.IsShipTypeAllowed(1)");
			Assert.IsTrue(edge.GetTravelCostMultiplier(1) == 1.0, "edge.GetTravelCostMultiplier(1) == 1.0");
		}

		[TestMethod]
		public void FullIntersection()
		{
			ShippingIssueManager issueManager = new ShippingIssueManager();
			RouteManager routeManager = new RouteManager(issueManager);
			APIShippingRestrictionGeometry[] restrictionGeometry =
			{
				new APIShippingRestrictionGeometry
				{
					geometry = new[] {new[] {500.0, 500.0}, new[] {1000.0, 500.0}, new[] {1000.0, 1000.0}, new[] {500.0, 1000.0}},
					geometry_id = 1, layer_id = 1, layer_types = new[] {1}
				}
			};

			APIShippingLaneGeometry[] laneGeometry = new[]
			{
				new APIShippingLaneGeometry{geometry = new []{new []{600.0, 600.0}}, geometry_id = 2, ship_type_ids = new []{ 1 }},
				new APIShippingLaneGeometry{geometry = new []{new []{600.0, 900.0}}, geometry_id = 2, ship_type_ids = new []{ 1 }}
			};

			routeManager.SetSimulationArea(m_simulationArea);
			routeManager.ImportRestrictions(restrictionGeometry, m_typeManager, 5000);
			routeManager.ImportLanes(laneGeometry, m_shipTypeManager, m_regionSettings);
			routeManager.RasterizeRestrictionMeshes(m_rasterizationRect, new Vector2(128.0f, 128.0f));
			ShippingPortManager portManager = new ShippingPortManager();

			routeManager.RebuildImplicitEdges(1000, portManager, 0.1);

			LaneEdge edge = routeManager.GetEdgeByIndex(0);
			Assert.IsTrue(edge.m_laneType == ELaneEdgeType.Implicit, "edge.m_laneType == ELaneEdgeType.Implicit");
			Assert.IsTrue(edge.IsShipTypeAllowed(1), "edge.IsShipTypeAllowed(1)");
			Assert.IsTrue(edge.GetTravelCostMultiplier(1) == 100.0, "edge.GetTravelCostMultiplier(1) == 100.0");
		}

		[TestMethod]
		public void PartialIntersection()
		{
			ShippingIssueManager issueManager = new ShippingIssueManager();
			RouteManager routeManager = new RouteManager(issueManager);
			APIShippingRestrictionGeometry[] restrictionGeometry =
			{
				new APIShippingRestrictionGeometry
				{
					geometry = new[] {new[] {500.0, 500.0}, new[] {1000.0, 500.0}, new[] {1000.0, 1000.0}, new[] {500.0, 1000.0}},
					geometry_id = 1, layer_id = 1, layer_types = new[] {1}
				}
			};

			APIShippingLaneGeometry[] laneGeometry = new[]
			{
				new APIShippingLaneGeometry{geometry = new []{new []{550.0, 750.0}}, geometry_id = 2, ship_type_ids = new []{ 1 }},
				new APIShippingLaneGeometry{geometry = new []{new []{550.0, 1750.0}}, geometry_id = 2, ship_type_ids = new []{ 1 }},
				new APIShippingLaneGeometry{geometry = new []{new []{750.0, 1750.0}}, geometry_id = 3, ship_type_ids = new []{ 1 }},
				new APIShippingLaneGeometry{geometry = new []{new []{750.0, 750.0}}, geometry_id = 3, ship_type_ids = new []{ 1 }}
			};

			routeManager.SetSimulationArea(m_simulationArea);
			routeManager.ImportRestrictions(restrictionGeometry, m_typeManager, 5000);
			routeManager.ImportLanes(laneGeometry, m_shipTypeManager, m_regionSettings);
			routeManager.RasterizeRestrictionMeshes(m_rasterizationRect, new Vector2(128.0f, 128.0f));
			ShippingPortManager portManager = new ShippingPortManager();

			routeManager.RebuildImplicitEdges(10000, portManager, 0.1);

			LaneEdge edge = routeManager.GetEdgeByIndex(1);
			Assert.IsTrue(edge.m_laneType == ELaneEdgeType.Implicit, "edge.m_laneType == ELaneEdgeType.Implicit");
			Assert.IsTrue(edge.IsShipTypeAllowed(1), "edge.IsShipTypeAllowed(1)");
			Assert.IsTrue(edge.GetTravelCostMultiplier(1) == 25.0, "edge.GetTravelCostMultiplier(1) == 25.0");

			edge = routeManager.GetEdgeByIndex(3);
			Assert.IsTrue(edge.m_laneType == ELaneEdgeType.Implicit, "edge.m_laneType == ELaneEdgeType.Implicit");
			Assert.IsTrue(edge.IsShipTypeAllowed(1), "edge.IsShipTypeAllowed(1)");
			Assert.IsTrue(edge.GetTravelCostMultiplier(1) == 25.0, "edge.GetTravelCostMultiplier(1) == 25.0");
		}

		[TestMethod]
		public void OverlappingIntersection()
		{
			ShippingIssueManager issueManager = new ShippingIssueManager();
			RouteManager routeManager = new RouteManager(issueManager);
			APIShippingRestrictionGeometry[] restrictionGeometry =
			{
				new APIShippingRestrictionGeometry
				{
					geometry = new[] {new[] {500.0, 500.0}, new[] {1000.0, 500.0}, new[] {1000.0, 1000.0}, new[] {500.0, 1000.0}},
					geometry_id = 1, layer_id = 1, layer_types = new[] {1}
				}
			};

			APIShippingLaneGeometry[] laneGeometry = new[]
			{
				new APIShippingLaneGeometry{geometry = new []{new []{750.0, 250.0}}, geometry_id = 2, ship_type_ids = new []{ 1 }},
				new APIShippingLaneGeometry{geometry = new []{new []{750.0, 1250.0}}, geometry_id = 2, ship_type_ids = new []{ 1 }},
			};

			routeManager.SetSimulationArea(m_simulationArea);
			routeManager.ImportRestrictions(restrictionGeometry, m_typeManager, 5000);
			routeManager.ImportLanes(laneGeometry, m_shipTypeManager, m_regionSettings);
			routeManager.RasterizeRestrictionMeshes(m_rasterizationRect, new Vector2(128.0f, 128.0f));
			ShippingPortManager portManager = new ShippingPortManager();

			routeManager.RebuildImplicitEdges(10000, portManager, 0.1);

			LaneEdge edge = routeManager.GetEdgeByIndex(0);
			Assert.IsTrue(edge.m_laneType == ELaneEdgeType.Implicit, "edge.m_laneType == ELaneEdgeType.Implicit");
			Assert.IsTrue(edge.IsShipTypeAllowed(1), "edge.IsShipTypeAllowed(1)");
			Assert.IsTrue(edge.GetTravelCostMultiplier(1) == 50.0, "edge.GetTravelCostMultiplier(1) == 75.0");
		}

		[TestMethod]
		public void OverlappingStartAndEndWithClearCenter()
		{
			ShippingIssueManager issueManager = new ShippingIssueManager();
			RouteManager routeManager = new RouteManager(issueManager);
			APIShippingRestrictionGeometry[] restrictionGeometry =
			{
				new APIShippingRestrictionGeometry
				{
					geometry = new[] {new[] {500.0, 500.0}, new[] {1000.0, 500.0}, new[] {1000.0, 1000.0}, new[] {500.0, 1000.0}, new[] {500.0, 500.0}},
					geometry_id = 1, layer_id = 1, layer_types = new[] {1}
				},
				new APIShippingRestrictionGeometry
				{
					geometry = new[] {new[] {1500.0, 500.0}, new[] {2000.0, 500.0}, new[] {2000.0, 1000.0}, new[] {1500.0, 1000.0}, new[] {1500.0, 500.0}},
					geometry_id = 2, layer_id = 1, layer_types = new[] {1}
				}
			};

			APIShippingLaneGeometry[] laneGeometry = new[]
			{
				new APIShippingLaneGeometry{geometry = new []{new []{750.0, 750.0}}, geometry_id = 10, ship_type_ids = new []{ 1 }},
				new APIShippingLaneGeometry{geometry = new []{new []{1750.0, 750.0}}, geometry_id = 10, ship_type_ids = new []{ 1 }},
			};

			routeManager.SetSimulationArea(m_simulationArea);
			routeManager.ImportRestrictions(restrictionGeometry, m_typeManager, 5000);
			routeManager.ImportLanes(laneGeometry, m_shipTypeManager, m_regionSettings);
			routeManager.RasterizeRestrictionMeshes(m_rasterizationRect, new Vector2(128.0f, 128.0f));
			ShippingPortManager portManager = new ShippingPortManager();

			routeManager.RebuildImplicitEdges(10000, portManager, 0.1);

			LaneEdge edge = routeManager.GetEdgeByIndex(0);
			Assert.IsTrue(edge.m_laneType == ELaneEdgeType.Implicit);
			Assert.IsTrue(edge.IsShipTypeAllowed(1));
			Assert.IsTrue(edge.GetTravelCostMultiplier(1) == 50.0);
		}
	}
}
