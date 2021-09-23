using System;

namespace SEL.KPI
{
	/// <summary>
	/// KPI that will post a value for each port how efficient the routes are that they have to all the other ports. 
	/// The efficiency is calculated as being the point-to-point distance between the ports over the actual distance the route is. (e.g. 100 distance vs 200 route distance would be 0.5 efficiency)
	/// The KPI value will have all route efficiencies combined, and all the individual route efficienciess should be displayed on the port information to prevent information clutter in the KPI window.
	/// </summary>
	class KPIPerPortTravelEfficiency : KPIBase
	{
		public override void Calculate(KPIInputData data)
		{
			foreach (ShippingPort port in data.portManager.GetAllPortsByType(EShippingPortType.DefinedPort))
			{
				//Then again, all the ports currently have an invalid country id assigned so this doesn't apply D:
				//if (port.owningCountryId == ShippingModel.INVALID_COUNTRY_ID)
				//{
				//	//We don't need KPI's for ports that are not assigned to a valid country;
				//	continue;
				//}

				float portEfficiency = 0.0f;
				int portRoutes = 0;
				foreach (ShipType shipType in port.GetAcceptingShipTypes())
				{
					foreach (Route route in data.routeManager.FindRoutesForPort(port, shipType.ShipTypeId))
					{
						double actualDistance = (route.ToVertex.position - route.FromVertex.position).Magnitude(); //As the crow flies
						double routeDistance = 0.0f;
						foreach (LaneEdge edge in route.GetRouteEdges())
						{
							routeDistance += edge.m_distance;
						}
						float efficiency = (float)(actualDistance / routeDistance);

						//@TODO: Submit per-port-per-route efficiencies here
						portEfficiency += efficiency;
						++portRoutes;
					}
				}

				if (portRoutes > 0)
				{
					float kpiPortEfficiency = portEfficiency / (float)portRoutes;
					SubmitData(string.Format("ShippingRouteEfficiency_{0}", port.PortName), data.month, (int)(kpiPortEfficiency * 100.0f), "%", port.OwningCountryId);
				}
			}
		}
	}
}
