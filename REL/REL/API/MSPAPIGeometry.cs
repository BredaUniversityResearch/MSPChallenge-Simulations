using System.Diagnostics.CodeAnalysis;

namespace REL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class MSPAPIGeometry
	{
		public uint geometry_id;
		public Vector2D[] geometry;
		public int geometry_type;
		//public int layer_id;
		//public int[] layer_types;
	}
}
