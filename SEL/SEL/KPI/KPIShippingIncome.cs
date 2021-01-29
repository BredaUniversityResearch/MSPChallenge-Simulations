using System;

namespace SEL.KPI
{
	class KPIShippingIncome : KPIBase
	{
		public override void Calculate(KPIInputData data)
		{
			foreach (ShippingPort port in data.portManager.GetAllPortsByType(EShippingPortType.DefinedPort))
			{
				float intensity = 0.0f;
				foreach (ShipType type in port.GetAcceptingShipTypes())
				{
					intensity += data.portManager.GetPortIntensity(port, type.ShipTypeId, data.month);
				}

				if (intensity > 0.0f)
				{
					SubmitData($"ShippingIncome_{port.PortName}", data.month, (int)intensity, "ship calls", port.OwningCountryId);
				}
			}
		}
	}
}
