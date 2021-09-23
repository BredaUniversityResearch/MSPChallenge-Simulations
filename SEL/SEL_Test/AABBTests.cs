using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEL;
using SEL.SpatialMapping;

namespace SEL_Test
{
	[TestClass]
	public class AABBTests
	{
		[TestMethod]
		public void IntersectTest()
		{
			AABB boundsA = new AABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 1.0));
			AABB boundsB = new AABB(new Vector2D(0.5, 0.5), new Vector2D(1.5, 1.5));

			Assert.IsTrue(boundsA.IntersectTest(boundsB) == EIntersectResult.Intersects);
			Assert.IsTrue(boundsB.IntersectTest(boundsA) == EIntersectResult.Intersects);
		}

		[TestMethod]
		public void ContainsTest()
		{
			AABB boundsA = new AABB(new Vector2D(0.0, 0.0), new Vector2D(2.0, 2.0));
			AABB boundsB = new AABB(new Vector2D(0.5, 0.5), new Vector2D(1.0, 1.0));

			Assert.IsTrue(boundsA.IntersectTest(boundsB) == EIntersectResult.Contained);
			Assert.IsTrue(boundsB.IntersectTest(boundsA) == EIntersectResult.Intersects);
		}

		[TestMethod]
		public void NoIntersectionTest()
		{
			AABB boundsA = new AABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 1.0));
			AABB boundsB = new AABB(new Vector2D(2.0, 0.0), new Vector2D(3.0, 1.0));

			Assert.IsTrue(boundsA.IntersectTest(boundsB) == EIntersectResult.NoIntersection);
			Assert.IsTrue(boundsB.IntersectTest(boundsA) == EIntersectResult.NoIntersection);
		}
	}
}
