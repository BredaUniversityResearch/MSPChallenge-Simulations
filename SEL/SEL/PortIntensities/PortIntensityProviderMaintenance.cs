using System;
using System.Collections.Generic;
using SEL.API;

namespace SEL.PortIntensities
{
	class PortIntensityProviderMaintenance: PortIntensityProviderBase
	{
		private const double SQUARE_METER_TO_SQUARE_KILOMETER = 1.0 / (1000 * 1000);

		private APISELRegionSettings.MaintenanceDestinationSettings m_intensitySettings = null;

		public void ImportIntensityData(APISELRegionSettings.MaintenanceDestinationSettings maintenanceDestinationSettings)
		{
			m_intensitySettings = maintenanceDestinationSettings;
		}

		public override IEnumerable<PortIntensityBase> CreateIntensities(ShippingPortManager portManager, ShipTypeManager shipTypeManager)
		{
			List<PortIntensityBase> portIntensities = new List<PortIntensityBase>();
			if (m_intensitySettings != null)
			{
				foreach (ShippingPort port in portManager.GetAllPortsByType(EShippingPortType.MaintenanceDestination))
				{
					PortIntensitySparse intensity = new PortIntensitySparse(port);
					foreach (ShipType shipType in shipTypeManager.GetShipTypes())
					{
						if (shipType.ShipRoutingType == Routing.EShipRoutingType.Maintenance)
						{
							intensity.SetIntensityValue(shipType.ShipTypeId, -1000, 0); //Make sure we have a base intensity of 0 before any construction starts.
							
							if (port.GeometryPoints == 1)
							{
								intensity.SetIntensityValue(shipType.ShipTypeId, port.ConstructionStartTime, (int)Math.Ceiling(m_intensitySettings.point_construction_intensity));
								intensity.SetIntensityValue(shipType.ShipTypeId, port.ConstructionEndTime, (int)Math.Ceiling(m_intensitySettings.point_intensity));
							}
							else
							{
								double area = port.GetSurfaceArea() * SQUARE_METER_TO_SQUARE_KILOMETER;
								intensity.SetIntensityValue(shipType.ShipTypeId, port.ConstructionStartTime, (int)Math.Ceiling(area * m_intensitySettings.construction_intensity_multiplier));
								intensity.SetIntensityValue(shipType.ShipTypeId, port.ConstructionEndTime, (int)Math.Ceiling(area * m_intensitySettings.base_intensity_per_square_km));
							}
						}
					}
					portIntensities.Add(intensity);
				}
			}
			return portIntensities;
		}
	}
}
