using System.Diagnostics.CodeAnalysis;

namespace SEL.API
{
	/// <summary>
	/// API class that holds all values required to post KPI results.
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	class APIKPIResult
	{
		public string name;		//Name of the KPI
		public int month;		//Month this KPI applies to
		public int value;		//The value of this KPI
		public string type;		//kpiCategory
		public string unit;		//the unit of the KPI.
		public int country = -1;//Country id this KPI applies to. -1 implies there's no country associated.
	}
}
