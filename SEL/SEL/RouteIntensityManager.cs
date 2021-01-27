using SEL.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SEL.PortIntensities;
using SEL.Routing;

namespace SEL
{
	/// <summary>
	/// Manager for holding and transforming all the port intensity data.
	/// We can use this to query for each time unit which routes have what intensity.
	/// </summary>
	class RouteIntensityManager
	{
		private List<RoutingEntry> m_routingTable = new List<RoutingEntry>();
		private List<RoutingComponentBase> m_routingComponents = new List<RoutingComponentBase>();
		private RoutingComponentRegularShipping m_regularShippingComponent;

		public RouteIntensityManager()
		{
			m_regularShippingComponent = new RoutingComponentRegularShipping();
			m_routingComponents.Add(m_regularShippingComponent);
			m_routingComponents.Add(new RoutingComponentMaintenance());
		}

		public void ImportConfiguredRoutes(APIConfiguredIntensityRoute[] configuredRouteIntensities)
		{
			m_regularShippingComponent.ImportConfiguredRoutes(configuredRouteIntensities);
		}

		public void RebuildRoutingEntries(ShipTypeManager shipTypeManager, RouteManager routeManager, ShippingPortManager portManager, int monthId)
		{
			m_routingTable.Clear();
			foreach (RoutingComponentBase routingComponent in m_routingComponents)
			{
				routingComponent.PreUpdate(routeManager);
			}

			foreach (ShipType type in shipTypeManager.GetShipTypes())
			{
				CreateRoutingEntriesForShipType(monthId, type, portManager);
			}
		}

		private void CreateRoutingEntriesForShipType(int monthId, ShipType shipType, ShippingPortManager portManager)
		{
			RoutingComponentBase component = m_routingComponents.Find(obj => obj.RoutingType == shipType.ShipRoutingType);
			if (component != null)
			{
				IEnumerable<RoutingEntry> routingEntries = component.CalculateRoutingEntries(monthId, shipType, this, portManager);
				m_routingTable.AddRange(routingEntries);
			}
			else
			{
				ErrorReporter.ReportError(EErrorSeverity.Error, "Unknown ship routing type " + shipType.ShipRoutingType);
			}
		}

		public IReadOnlyList<RoutingEntry> GetCurrentRoutes()
		{
			return m_routingTable.AsReadOnly();
		}

		public RouteIntensity[] GetAllAbsoluteRouteIntensities(int monthId, ShippingPortManager portManager)
		{
			List<RouteIntensity> intensities = new List<RouteIntensity>();

			foreach (RoutingEntry entry in m_routingTable)
			{
				int sourcePortIntensity = portManager.GetPortIntensity(entry.sourcePort, entry.shipTypeId, monthId);
				int shipIntensityValue = (int)(entry.sourcePortPercentage * sourcePortIntensity);
				if (shipIntensityValue > 0)
				{
					RouteIntensity intensity = new RouteIntensity(entry.sourcePort, entry.destinationPort, entry.shipTypeId, shipIntensityValue);
					intensities.Add(intensity);
				}
			}

			return intensities.ToArray();
		}

		public IEnumerable<APIConfiguredIntensityRoute> SelectActiveConfiguredRoutes(APIConfiguredIntensityRoute[] configuredRouteIntensities, int monthId)
		{
			Dictionary<int, APIConfiguredIntensityRoute> activeRoutes = new Dictionary<int, APIConfiguredIntensityRoute>(configuredRouteIntensities.Length);

			foreach (APIConfiguredIntensityRoute route in configuredRouteIntensities)
			{
				if (route.start_time > monthId)
				{
					//Immediately reject the route since it is not started yet.
					continue;
				}
				int routeHash = GetConfiguredRouteHash(route);

				APIConfiguredIntensityRoute presentRoute;
				if (activeRoutes.TryGetValue(routeHash, out presentRoute))
				{
					//Route already present, overwrite if our start time is higher than the end time.
					DebugVerifyNoHashCollision(route, presentRoute);
					if (presentRoute.start_time < route.start_time)
					{
						activeRoutes[routeHash] = route;
					}
				}
				else
				{
					activeRoutes.Add(routeHash, route);
				}
			}

			return activeRoutes.Values;
		}

		private int GetConfiguredRouteHash(APIConfiguredIntensityRoute route)
		{
			//Lolololololol
			StringBuilder uniqueString = new StringBuilder(256);
			uniqueString.Append(route.source_port_id);
			uniqueString.Append(route.destination_port_id);
			uniqueString.Append(route.ship_type_id);

			return uniqueString.ToString().GetHashCode();
		}

		[System.Diagnostics.Conditional("DEBUG")]
		private void DebugVerifyNoHashCollision(APIConfiguredIntensityRoute routeA, APIConfiguredIntensityRoute routeB)
		{
			if (routeA.source_port_id != routeB.source_port_id ||
				routeA.destination_port_id != routeB.destination_port_id ||
				routeA.ship_type_id != routeB.ship_type_id)
			{
				throw new Exception("We have a winner! Hash collision gallore!");
			}
		}
	}
}