using System.Collections.Generic;

namespace SEL.PortIntensities
{
	abstract class PortIntensityProviderBase
	{
		public abstract IEnumerable<PortIntensityBase> CreateIntensities(ShippingPortManager portManager, ShipTypeManager shipTypeManager);
	}
}
