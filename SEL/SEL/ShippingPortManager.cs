using System.Collections.Generic;
using SEL.API;
using SEL.PortIntensities;

namespace SEL
{
	class ShippingPortManager
	{
		private Dictionary<ShippingPort, PortIntensityBase> m_portIntensities = new Dictionary<ShippingPort, PortIntensityBase>(); //Intensity of the ports indexed by port geometry id.
		private List<PortIntensityProviderBase> m_portIntensityProviders = new List<PortIntensityProviderBase>();
		private PortIntensityProviderDefinedIntensities m_definedIntensities;
		private PortIntensityProviderMaintenance m_maintenanceIntensity;

		private List<ShippingPort> m_availablePorts = new List<ShippingPort>();

		public ShippingPortManager()
		{
			m_definedIntensities = new PortIntensityProviderDefinedIntensities();
			m_portIntensityProviders.Add(m_definedIntensities);
			m_maintenanceIntensity = new PortIntensityProviderMaintenance();
			m_portIntensityProviders.Add(m_maintenanceIntensity);
		}

		public void ImportPorts(APIShippingPortGeometry[] shippingPortGeometry, RouteManager routeManager)
		{
			if (shippingPortGeometry != null)
			{
				foreach (APIShippingPortGeometry port in shippingPortGeometry)
				{
					ShippingPort portInstance = new ShippingPort(port.center.x, port.center.y, port.port_id, port, port.port_type);
					portInstance.SetPathingVertex(routeManager.CreatePortVertex(port.center));
					m_availablePorts.Add(portInstance);
				}
			}
		}

		public void ImportIntensityData(APIShippingPortIntensity[] intensityData, APISELRegionSettings.MaintenanceDestinationSettings maintenanceDestinationSettings, ShippingPortManager portManager, ShipTypeManager shipTypeManager)
		{
			foreach (APIShippingPortIntensity portIntensity in intensityData)
			{
				ShippingPort sourcePort = portManager.FindShippingPortByName(portIntensity.port_id);
				if (sourcePort != null)
				{
					foreach (APIShippingPortIntensity.ShipTypeIntensity shipType in portIntensity.ship_intensity_values)
					{
						ShipType shipTypeData = shipTypeManager.FindShipTypeById(shipType.ship_type_id);
						if (shipTypeData != null)
						{
							sourcePort.SetAcceptsShipType(shipTypeData);
						}
						else
						{
							ErrorReporter.ReportError(EErrorSeverity.Error,
								$"Could not find ship type with ID {shipType.ship_type_id} for port intensity on port {portIntensity.port_id}");
						}
					}
				}
				else
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						$"Could not find port with ID {portIntensity.port_id} for port intensity");
				}
			}

			m_definedIntensities.ImportIntensityData(intensityData, portManager);
			if (maintenanceDestinationSettings != null)
			{
				m_maintenanceIntensity.ImportIntensityData(maintenanceDestinationSettings);
			}
		}

		public void UpdatePortIntensities(int monthId, ShippingPortManager portManager, ShipTypeManager shipTypeManager)
		{
			m_portIntensities.Clear();
			foreach (PortIntensityProviderBase intensityProvider in m_portIntensityProviders)
			{
				foreach (PortIntensityBase intensity in intensityProvider.CreateIntensities(portManager, shipTypeManager))
				{
					m_portIntensities.Add(intensity.TargetPort, intensity);
				}
			}
		}

		public ShippingPort FindShippingPortByName(string shippingPortName)
		{
			return m_availablePorts.Find(obj => obj.PortName == shippingPortName);
		}

		public List<ShippingPort> GetAllPortsByType(EShippingPortType portType)
		{
			return m_availablePorts.FindAll(obj => obj.ShippingPortType == portType);
		}

		public int GetPortIntensity(ShippingPort shippingPort, byte shipTypeId, int monthId)
		{
			int result = 0;
			PortIntensityBase intensity;
			if (m_portIntensities.TryGetValue(shippingPort, out intensity))
			{
				result = intensity.GetShipIntensityValue(shipTypeId, monthId);
			}
			return result;
		}

		//Returns the a pair of the portId as key and port intensity as value.
		public IEnumerable<KeyValuePair<ShippingPort, PortIntensityBase>> GetPortIntensities()
		{
			return m_portIntensities;
		}

		public int GetPortIntensityCount()
		{
			return m_portIntensities.Count;
		}
	}
}
