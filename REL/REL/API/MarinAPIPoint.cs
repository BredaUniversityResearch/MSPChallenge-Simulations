using System.Diagnostics.CodeAnalysis;

namespace REL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MarinAPIPoint
	{
		public ushort point_id;
		public double lat;
		public double lon;
	}
}
