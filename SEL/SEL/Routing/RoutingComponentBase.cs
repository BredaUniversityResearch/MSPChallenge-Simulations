using System.Collections.Generic;

namespace SEL.Routing
{
	/// <summary>
	/// Component that generates a routing table. 
	/// </summary>
	abstract class RoutingComponentBase
	{
		public EShipRoutingType RoutingType
		{
			get;
		}

		protected RoutingComponentBase(EShipRoutingType routingType)
		{
			RoutingType = routingType;
		}

		//Called once before the calls to CalculateRoutingEntries might be made.
		public virtual void PreUpdate(RouteManager routeManager)
		{
		}

		public abstract IEnumerable<RoutingEntry> CalculateRoutingEntries(int monthId, ShipType shipTypew, RouteIntensityManager routeIntensityManager, ShippingPortManager portManager);
	}
}
