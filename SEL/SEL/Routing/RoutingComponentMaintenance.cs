using System.Collections.Generic;
using SEL.API;

namespace SEL.Routing
{
	class RoutingComponentMaintenance: RoutingComponentBase
	{
		public RoutingComponentMaintenance() 
			: base(EShipRoutingType.Maintenance)
		{
		}

		public override IEnumerable<RoutingEntry> CalculateRoutingEntries(int monthId, ShipType shipType, RouteIntensityManager routeIntensityManager, ShippingPortManager portManager)
		{
			//This is intentionally the wrong way around. 
			List<ShippingPort> sourcePorts = portManager.GetAllPortsByType(EShippingPortType.MaintenanceDestination);
			List<ShippingPort> destinationPorts = portManager.GetAllPortsByType(EShippingPortType.DefinedPort);

			List<RoutingEntry> entries = new List<RoutingEntry>(sourcePorts.Count);
			foreach (ShippingPort port in sourcePorts)
			{
				ShippingPort destination = GetNearestDestination(port, destinationPorts);
				if (destination != null)
				{
					RoutingEntry entry = new RoutingEntry(shipType.ShipTypeId, port, destination, 1.0f);
					entries.Add(entry);
				}
			}

			return entries;
		}

		private ShippingPort GetNearestDestination(ShippingPort source, List<ShippingPort> destinationPorts)
		{
			double nearestDistanceSqr = double.MaxValue;
			ShippingPort nearestValue = null;
			foreach (ShippingPort destination in destinationPorts)
			{
				Vector2D deltaPosition = destination.Center - source.Center;
				double distanceSqr = deltaPosition.MagnitudeSqr();
				if (distanceSqr < nearestDistanceSqr)
				{
					nearestValue = destination;
					nearestDistanceSqr = distanceSqr;
				}
			}
			if (nearestValue == null)
			{
				ErrorReporter.ReportError(EErrorSeverity.Error, "Nearest port is NULL. No shipping ports defined?");
			}
			return nearestValue;
		}
	}
}
