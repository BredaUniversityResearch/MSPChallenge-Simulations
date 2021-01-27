using SEL.API;
using System.Collections.Generic;
using System;

namespace SEL.KPI
{
	/// <summary>
	/// Manager which contains all KPI's that need to be calculated every time tick.
	/// </summary>
	class KPIManager
	{
		private IApiConnector m_apiConnector = null;
		private List<KPIBase> m_availableKPI = new List<KPIBase>();
		private List<APIKPIResult> m_KPIResultQueue = new List<APIKPIResult>();

		public KPIManager(IApiConnector connector)
		{
			m_apiConnector = connector;

			AddKPIInstance(new KPIShippingIntensity());
			AddKPIInstance(new KPIShippingIncome());
			AddKPIInstance(new KPIPerPortTravelEfficiency());
			AddKPIInstance(new KPIShippingRisk());
			AddKPIInstance(new KPIPerLaneGeometryShippingIntensity());
		}

		private void AddKPIInstance(KPIBase instance)
		{
			instance.SetKPIManager(this);
			m_availableKPI.Add(instance);
		}

		public void CalculateKPIs(KPIInputData a_data)
		{
			foreach (KPIBase kpi in m_availableKPI)
			{
				kpi.Calculate(a_data);
			}
		}

		public void QueueKPIResultValue(string kpiName, int kpiValue, string kpiCategory, string kpiUnit, int country = -1)
		{
			APIKPIResult result = new APIKPIResult
			{
				name = kpiName,
				value = kpiValue,
				type = kpiCategory,
				unit = kpiUnit,
				country = country
			};
			m_KPIResultQueue.Add(result);
		}

		public IApiConnector GetApiConnector()
		{
			return m_apiConnector;
		}

		public void SubmitKPIResults(IApiConnector apiConnector)
		{
			apiConnector.BatchPostKPI(m_KPIResultQueue);
			m_KPIResultQueue.Clear();
		}
	}
}
