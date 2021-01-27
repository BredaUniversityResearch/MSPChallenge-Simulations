using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace REL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MSPAPIDate
	{
		[JsonRequired]
		public int month_of_year;
		[JsonRequired]
		public int year;
	}
}