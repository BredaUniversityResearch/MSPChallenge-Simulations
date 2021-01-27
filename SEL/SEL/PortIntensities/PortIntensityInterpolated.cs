using SEL.Util;

namespace SEL.PortIntensities
{
	class PortIntensityInterpolated: PortIntensityBase
	{
		public PortIntensityInterpolated(ShippingPort targetPort) 
			: base(targetPort)
		{
		}

		protected override IValueMapping<int, int> CreateNewValueMapping()
		{
			return new InterpolatedValueMapping<int, int>();
		}
	}
}
