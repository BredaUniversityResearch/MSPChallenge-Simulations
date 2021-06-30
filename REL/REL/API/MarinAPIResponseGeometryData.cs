using System.Diagnostics.CodeAnalysis;

namespace REL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MarinAPIResponseGeometryData
	{
		public uint geometry_id;
		public int segment_id;
		public byte ship_type;
		public double contacts;
	}
}