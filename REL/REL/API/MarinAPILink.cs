using System.Diagnostics.CodeAnalysis;

namespace REL
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MarinAPILink
	{
		public ushort link_id;
		public ushort point_id_start;
		public ushort point_id_end;
		public float link_width;
	}
}