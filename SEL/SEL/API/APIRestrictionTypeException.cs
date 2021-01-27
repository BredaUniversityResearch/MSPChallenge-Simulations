using System.Diagnostics.CodeAnalysis;

namespace SEL.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	class APIRestrictionTypeException
	{
		public int layer_id = -1;
		public int layer_type_id = -1;
		public int[] allowed_ship_type_ids = null;
		public float[] cost_multipliers = null;
	}
}
