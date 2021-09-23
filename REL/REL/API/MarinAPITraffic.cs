using System.Diagnostics.CodeAnalysis;

namespace REL
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MarinAPITraffic
	{
		public ushort link_id;
		public byte ship_type;
		public int intensity;
	}
}