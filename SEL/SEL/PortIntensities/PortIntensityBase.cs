using System.Collections.Generic;
using SEL.Util;

namespace SEL.PortIntensities
{
	/// <summary>
	/// Base class for holding port intensity information.
	/// </summary>
	abstract class PortIntensityBase
	{
		public ShippingPort TargetPort { get; }
		//Intensities of this port grouped by ship type and sorted by implementation date.
		public Dictionary<int, IValueMapping<int, int>> m_intensityValues = new Dictionary<int, IValueMapping<int, int>>();

		protected PortIntensityBase(ShippingPort targetPort)
		{
			TargetPort = targetPort;
		}

		public void SetIntensityValue(int shipTypeId, int timeMonth, int intensity)
		{
			IValueMapping<int, int> mapping;
			if (!m_intensityValues.TryGetValue(shipTypeId, out mapping))
			{
				mapping = CreateNewValueMapping();
				m_intensityValues.Add(shipTypeId, mapping);
			}
			mapping.Add(timeMonth, intensity);
		}

		public int GetShipIntensityValue(int shipTypeId, int timeMonth)
		{
			int result = 0;
			IValueMapping<int, int> mapping;
			if (m_intensityValues.TryGetValue(shipTypeId, out mapping))
			{
				result = mapping.Map(timeMonth);
			}
			return result;
		}

		protected abstract IValueMapping<int, int> CreateNewValueMapping();
	}
}
