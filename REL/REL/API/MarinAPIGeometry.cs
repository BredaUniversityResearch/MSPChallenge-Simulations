using System.Diagnostics.CodeAnalysis;

namespace REL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MarinAPIGeometry
	{
		public uint geometry_id;
		public int geometry_type;
		public Vector2D[] geometry;
	}
}