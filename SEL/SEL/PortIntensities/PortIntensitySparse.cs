using SEL.Util;

namespace SEL.PortIntensities
{
	class PortIntensitySparse: PortIntensityBase
	{
		public PortIntensitySparse(ShippingPort targetPort) 
			: base(targetPort)
		{
		}

		protected override IValueMapping<int, int> CreateNewValueMapping()
		{
			return new SparseValueMapping();
		}
	}
}
