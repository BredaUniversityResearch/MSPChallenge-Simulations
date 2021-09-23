using System.Collections.Generic;
using SEL.API;

namespace SEL.PortIntensities
{
	class PortIntensityProviderDefinedIntensities : PortIntensityProviderBase
	{
		private List<PortIntensityInterpolated> m_intensities = new List<PortIntensityInterpolated>();

		public void ImportIntensityData(APIShippingPortIntensity[] intensityData, ShippingPortManager shippingPortManager)
		{
			foreach (APIShippingPortIntensity data in intensityData)
			{
				PortIntensityInterpolated intensity = new PortIntensityInterpolated(shippingPortManager.FindShippingPortByName(data.port_id));

				foreach (APIShippingPortIntensity.ShipTypeIntensity shipTypeIntensity in data.ship_intensity_values)
				{
					intensity.SetIntensityValue(shipTypeIntensity.ship_type_id, shipTypeIntensity.start_time, shipTypeIntensity.ship_intensity);
				}
				m_intensities.Add(intensity);
			}
		}

		public override IEnumerable<PortIntensityBase> CreateIntensities(ShippingPortManager portManager, ShipTypeManager shipTypeManager)
		{
			return m_intensities;
		}
	}
}
