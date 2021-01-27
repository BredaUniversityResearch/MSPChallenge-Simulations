using SEL.API;

namespace SEL.KPI
{
	/// <summary>
	/// Base interface to a KPI calculation.
	/// </summary>
	abstract class KPIBase
	{
		protected KPIManager kpiManager { get; private set; }

		public void SetKPIManager(KPIManager a_kpiManager)
		{
			kpiManager = a_kpiManager;
		}

		public abstract void Calculate(KPIInputData data);

		protected void SubmitData(string kpiName, int kpiValue, string kpiUnit, int country = -1)
		{
			kpiManager.QueueKPIResultValue(kpiName, kpiValue, "SHIPPING", kpiUnit, country);
		}
	}
}
