namespace SEL.KPI
{
	/// <summary>
	/// KPI for calculating the total shipping intensity in the current area. 
	/// </summary>
	class KPIShippingIntensity: KPIBase
	{
		public override void Calculate(KPIInputData data)
		{
			float shippingIntensity = 0.0f;
			foreach (ShippingPort port in data.portManager.GetAllPortsByType(EShippingPortType.DefinedPort))
			{
				foreach (ShipType shipType in port.GetAcceptingShipTypes())
				{
					shippingIntensity += data.portManager.GetPortIntensity(port, shipType.ShipTypeId, data.month);
				}
			}

			SubmitData("ShippingIntensity", (int)shippingIntensity, "ship calls");
		}
	}
}
