using System;

namespace SEL.KPI
{
	/// <summary>
	/// KPI class for calculating the shipping risk into a single value.
	/// </summary>
	class KPIShippingRisk : KPIBase
	{
		public override void Calculate(KPIInputData data)
		{
			IntensityMapGraphic[] riskmaps = data.rasterManager.GetLatestResultsForType(EHeatmapType.Riskmap);
			int kpiResultValue = 0;
			foreach (IntensityMapGraphic graphic in riskmaps)
			{
				foreach (int rasterValue in graphic.GetRasterValues())
				{
					kpiResultValue += rasterValue;
				}
			}

			SubmitData("ShippingRisk", kpiResultValue, "");
		}
	}
}
