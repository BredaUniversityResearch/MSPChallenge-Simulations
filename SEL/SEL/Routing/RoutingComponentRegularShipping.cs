using System;
using System.Collections.Generic;
using SEL.API;
using SEL.PortIntensities;

namespace SEL.Routing
{
	class RoutingComponentRegularShipping: RoutingComponentBase
	{
		private const float DISTRIBUTE_TRESHOLD = 0.001f;

		private APIConfiguredIntensityRoute[] m_configuredRouteIntensities = null;

		public RoutingComponentRegularShipping()
			: base(EShipRoutingType.RegularShipping)
		{
		}

		public void ImportConfiguredRoutes(APIConfiguredIntensityRoute[] configuredRouteIntensities)
		{
			m_configuredRouteIntensities = configuredRouteIntensities;
		}

		public override IEnumerable<RoutingEntry> CalculateRoutingEntries(int monthId, ShipType shipType, RouteIntensityManager routeIntensityManager, ShippingPortManager portManager)
		{
			List<RoutingEntry> routingEntries = new List<RoutingEntry>();
			VerifyDataConfiguration(portManager);
			BuildRoutingEntriesForShipType(shipType.ShipTypeId, monthId, routeIntensityManager, portManager, routingEntries);
			return routingEntries;
		}

		private void VerifyDataConfiguration(ShippingPortManager portManager)
		{
			//Double check that we know all the ports that are referenced in the configured routes.
			foreach (APIConfiguredIntensityRoute route in m_configuredRouteIntensities)
			{
				ShippingPort sourcePort = portManager.FindShippingPortByName(route.source_port_id);
				if (sourcePort == null)
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						$"Unknown port ID \"{route.source_port_id}\" as source_port_id (destination \"{route.destination_port_id}\") in configured route.");
				}
				ShippingPort destinationPort = portManager.FindShippingPortByName(route.destination_port_id);
				if (destinationPort == null)
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						$"Unknown port ID \"{route.destination_port_id}\" as destination_port_id (source \"{route.source_port_id}\") in configured route.");
				}
			}
		}

		private void BuildRoutingEntriesForShipType(byte shipType, int monthId, RouteIntensityManager routeIntensityManager, ShippingPortManager portManager, List<RoutingEntry> resultEntries)
		{
			IEnumerable<APIConfiguredIntensityRoute> activeConfiguredRoutes = routeIntensityManager.SelectActiveConfiguredRoutes(m_configuredRouteIntensities, monthId);

			List<ShippingPort> regularShippingPorts = portManager.GetAllPortsByType(EShippingPortType.DefinedPort);
			foreach (ShippingPort sourcePort in regularShippingPorts)
			{
				List<RoutingEntry> intensityList = new List<RoutingEntry>(regularShippingPorts.Count);

				HashSet<string> visitedDestinations = new HashSet<string>();
				float remainingRoutingPercentage = 1.0f; //The remaining percentage of ships we can still route from here.
														 //First we route all the boats in configured routes to their destinations.
				foreach (APIConfiguredIntensityRoute routeIntensity in activeConfiguredRoutes)
				{
					if (routeIntensity.ship_type_id == shipType && routeIntensity.source_port_id == sourcePort.PortName)
					{
						intensityList.Add(new RoutingEntry(shipType, sourcePort, portManager.FindShippingPortByName(routeIntensity.destination_port_id), routeIntensity.intensity_percentage));
						visitedDestinations.Add(routeIntensity.destination_port_id);
						remainingRoutingPercentage -= routeIntensity.intensity_percentage;
						if (remainingRoutingPercentage < -1e5f) // Yay floating point inaccuracies.
						{
							throw new Exception(string.Format("Configuration invalid. Configured route intensity for port {0} accounts to something bigger than 1.0", routeIntensity.source_port_id));
						}
					}
				}
				resultEntries.AddRange(intensityList);

				if (remainingRoutingPercentage > DISTRIBUTE_TRESHOLD)
				{
					List<RoutingEntry> evenDistributedIntensityList = new List<RoutingEntry>(portManager.GetPortIntensityCount());
					foreach (KeyValuePair<ShippingPort, PortIntensityBase> destinationIntensityKvp in portManager.GetPortIntensities())
					{
						if (destinationIntensityKvp.Key.ShippingPortType != EShippingPortType.DefinedPort ||
							destinationIntensityKvp.Key.PortName == sourcePort.PortName ||
							visitedDestinations.Contains(destinationIntensityKvp.Key.PortName))
						{
							continue;
						}

						int intensity = destinationIntensityKvp.Value.GetShipIntensityValue(shipType, 0);
						if (intensity > 0)
						{
							RoutingEntry entry = new RoutingEntry(shipType, sourcePort, destinationIntensityKvp.Key, (float)intensity);
							evenDistributedIntensityList.Add(entry);
						}
					}
					NormalizeList(ref evenDistributedIntensityList, remainingRoutingPercentage);
					resultEntries.AddRange(evenDistributedIntensityList);
				}
			}
		}

		private void NormalizeList(ref List<RoutingEntry> intensityValues, float normalizeToValue)
		{
			float accumulatedValue = 0.0f;
			foreach (RoutingEntry value in intensityValues)
			{
				accumulatedValue += value.sourcePortPercentage;
			}

			float rcpAccumulatedValue = (1.0f / accumulatedValue) * normalizeToValue;
			for (int i = 0; i < intensityValues.Count; ++i)
			{
				intensityValues[i].sourcePortPercentage = intensityValues[i].sourcePortPercentage * rcpAccumulatedValue;
			}
		}
	}
}
