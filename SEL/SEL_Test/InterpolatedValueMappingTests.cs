using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEL.Util;

namespace SEL_Test
{
	[TestClass]
	public class InterpolatedValueMappingTests
	{
		[TestMethod]
		public void MappingTest()
		{
			InterpolatedValueMapping<float, float> mapper = new InterpolatedValueMapping<float, float>();
			mapper.Add(0.0f, 1.0f);
			mapper.Add(1.0f, 2.0f);

			Assert.IsTrue(mapper.Map(0.0f) == 1.0f);
			Assert.IsTrue(mapper.Map(1.0f) == 2.0f);
		}

		[TestMethod]
		public void InterpolatedMappingTest()
		{
			InterpolatedValueMapping<float, float> mapper = new InterpolatedValueMapping<float, float>();
			mapper.Add(0.0f, 1.0f);
			mapper.Add(1.0f, 2.0f);

			Assert.IsTrue(mapper.Map(0.5f) == 1.5f);
		}

		[TestMethod]
		public void OutOfBoundsMapping()
		{
			InterpolatedValueMapping<float, float> mapper = new InterpolatedValueMapping<float, float>();
			mapper.Add(1.0f, 10.0f);
			mapper.Add(2.0f, 11.0f);

			Assert.IsTrue(mapper.Map(0.0f) == 10.0f);
			Assert.IsTrue(mapper.Map(1.0f) == 10.0f);
			Assert.IsTrue(mapper.Map(2.0f) == 11.0f);
			Assert.IsTrue(mapper.Map(3.0f) == 11.0f);
		}
	}
}
