using System.Diagnostics.CodeAnalysis;

namespace REL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MarinAPIInput
	{
		public MarinAPIDate date;
		public MarinAPIPoint[] points;
		public MarinAPILink[] links;
		public MarinAPITraffic[] traffic;
		public MarinAPIGeometry[] restriction_data;
	}
}
