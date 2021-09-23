using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEL.Util;

namespace SEL_Test
{
	[TestClass]
	public class SparseValueMappingTests
	{
		[TestMethod]
		public void MappingTest()
		{
			SparseValueMapping mapper = new SparseValueMapping();
			mapper.Add(10, 100);
			mapper.Add(20, 200);
			mapper.Add(30, 300);
			mapper.Add(40, 400);

			Assert.IsTrue(mapper.Map(10) == 100);
			Assert.IsTrue(mapper.Map(20) == 200);

			Assert.IsTrue(mapper.Map(29) == 200);
			Assert.IsTrue(mapper.Map(30) == 300);
			Assert.IsTrue(mapper.Map(31) == 300);
		}

		[TestMethod]
		public void OutOfBoundsTest()
		{
			SparseValueMapping mapper = new SparseValueMapping();

			mapper.Add(10, 100);
			mapper.Add(20, 200);

			Assert.IsTrue(mapper.Map(0) == 100);
			Assert.IsTrue(mapper.Map(30) == 200);
		}
	}
}
