using SEL.API;
using System.Collections.Generic;

namespace SEL.KPI
{
	/// <summary>
	/// Data packet for the KPI calculations.
	/// </summary>
	class KPIInputData
	{
		public int month { get; private set; }
		public ShippingPortManager portManager { get; private set; }
		public RouteIntensityManager intensityManager { get; private set; }
		public RouteManager routeManager { get; private set; }
		public RasterOutputManager rasterManager { get; private set; }

		public KPIInputData(int month, ShippingPortManager shippingPortManager, RouteIntensityManager intensityManager, RouteManager routeManager, RasterOutputManager rasterManager)
		{
			this.month = month;
			portManager = shippingPortManager;
			this.intensityManager = intensityManager;
			this.routeManager = routeManager;
			this.rasterManager = rasterManager;
		}
	}
}
