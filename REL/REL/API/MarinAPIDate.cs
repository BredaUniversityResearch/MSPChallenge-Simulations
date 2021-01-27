using System.Diagnostics.CodeAnalysis;

namespace REL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MarinAPIDate
	{
		public readonly int month; //[1..12]
		public readonly int year; //Actual "real time" year

		public MarinAPIDate(int a_month, int a_year)
		{
			month = a_month;
			year = a_year;
		}

	}
}
