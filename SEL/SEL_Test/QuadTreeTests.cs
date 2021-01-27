using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEL;
using SEL.SpatialMapping;

namespace SEL_Test
{
	[TestClass]
	public class QuadTreeTests
	{
		[TestMethod]
		public void TestSingleObject()
		{
			QuadTree<int> tree = new QuadTree<int>(new AABB(new Vector2D(0.0, 0.0), new Vector2D(10.0, 10.0)));
			tree.Insert(new AABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 1.0)), 1);
		}

		[TestMethod]
		public void TestSpatialQuery()
		{
			QuadTree<int> tree = new QuadTree<int>(new AABB(new Vector2D(0.0, 0.0), new Vector2D(10.0, 10.0)));
			tree.Insert(new AABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 1.0)), 1);
			tree.Insert(new AABB(new Vector2D(2.0, 0.0), new Vector2D(3.0, 0.0)), 2);
			tree.Insert(new AABB(new Vector2D(0.0, 2.0), new Vector2D(0.0, 3.0)), 3);
			tree.Insert(new AABB(new Vector2D(0.0, 2.0), new Vector2D(2.0, 2.0)), 3);

			QuadTreeSpatialQueryResult<int> result = tree.Query(new SpatialQueryAABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 1.0)));

			Assert.IsTrue(result.GetResultCount() == 1);

			tree.Insert(new AABB(new Vector2D(0.5, 0.5), new Vector2D(1.5, 1.5)), 4);

			result = tree.Query(new SpatialQueryAABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 1.0)));
			Assert.IsTrue(result.GetResultCount() == 2);

		}

		[TestMethod]
		public void TestSubdivision()
		{
			QuadTree<int> tree = new QuadTree<int>(new AABB(new Vector2D(0.0, 0.0), new Vector2D(8.0, 8.0)));
			tree.Insert(new AABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 1.0)), 1);
			tree.Insert(new AABB(new Vector2D(1.0, 0.0), new Vector2D(1.0, 1.0)), 2);
			tree.Insert(new AABB(new Vector2D(0.0, 1.0), new Vector2D(1.0, 1.0)), 3);
			tree.Insert(new AABB(new Vector2D(1.0, 1.0), new Vector2D(1.0, 1.0)), 4);

			tree.Insert(new AABB(new Vector2D(0.0, 0.0), new Vector2D(2.0, 1.0)), 5);
			tree.Insert(new AABB(new Vector2D(1.0, 0.0), new Vector2D(2.0, 1.0)), 6);
			tree.Insert(new AABB(new Vector2D(0.0, 1.0), new Vector2D(2.0, 1.0)), 7);
			tree.Insert(new AABB(new Vector2D(1.0, 1.0), new Vector2D(2.0, 1.0)), 8);

			tree.Insert(new AABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 2.0)), 9);
			tree.Insert(new AABB(new Vector2D(1.0, 0.0), new Vector2D(1.0, 2.0)), 10);
			tree.Insert(new AABB(new Vector2D(0.0, 1.0), new Vector2D(1.0, 2.0)), 11);
			tree.Insert(new AABB(new Vector2D(1.0, 1.0), new Vector2D(1.0, 2.0)), 12);

			tree.Insert(new AABB(new Vector2D(0.0, 0.0), new Vector2D(2.0, 2.0)), 13);
			tree.Insert(new AABB(new Vector2D(1.0, 0.0), new Vector2D(2.0, 2.0)), 14);
			tree.Insert(new AABB(new Vector2D(0.0, 1.0), new Vector2D(2.0, 2.0)), 15);
			tree.Insert(new AABB(new Vector2D(1.0, 1.0), new Vector2D(2.0, 2.0)), 16);

			Assert.IsTrue(tree.GetPopulatedNodeCount() == 5);
		}

		[TestMethod]
		public void TestIteration()
		{
			QuadTree<int> tree = new QuadTree<int>(new AABB(new Vector2D(0.0, 0.0), new Vector2D(8.0, 8.0)));
			tree.Insert(new AABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 1.0)), 1);
			tree.Insert(new AABB(new Vector2D(1.0, 0.0), new Vector2D(1.0, 1.0)), 2);
			tree.Insert(new AABB(new Vector2D(0.0, 1.0), new Vector2D(1.0, 1.0)), 3);
			tree.Insert(new AABB(new Vector2D(1.0, 1.0), new Vector2D(1.0, 1.0)), 4);

			tree.Insert(new AABB(new Vector2D(0.0, 0.0), new Vector2D(2.0, 1.0)), 5);
			tree.Insert(new AABB(new Vector2D(1.0, 0.0), new Vector2D(2.0, 1.0)), 6);
			tree.Insert(new AABB(new Vector2D(0.0, 1.0), new Vector2D(2.0, 1.0)), 7);
			tree.Insert(new AABB(new Vector2D(1.0, 1.0), new Vector2D(2.0, 1.0)), 8);

			tree.Insert(new AABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 2.0)), 9);
			tree.Insert(new AABB(new Vector2D(1.0, 0.0), new Vector2D(1.0, 2.0)), 10);
			tree.Insert(new AABB(new Vector2D(0.0, 1.0), new Vector2D(1.0, 2.0)), 11);
			tree.Insert(new AABB(new Vector2D(1.0, 1.0), new Vector2D(1.0, 2.0)), 12);

			tree.Insert(new AABB(new Vector2D(0.0, 0.0), new Vector2D(2.0, 2.0)), 13);
			tree.Insert(new AABB(new Vector2D(1.0, 0.0), new Vector2D(2.0, 2.0)), 14);
			tree.Insert(new AABB(new Vector2D(0.0, 1.0), new Vector2D(2.0, 2.0)), 15);
			tree.Insert(new AABB(new Vector2D(1.0, 1.0), new Vector2D(2.0, 2.0)), 16);

			int entityCount = 0;
			foreach (QuadTreeEntity<int> entity in tree.GetEntityIterator(null))
			{
				Assert.IsTrue(entity != null);
				++entityCount;
			}
			Assert.IsTrue(entityCount == 16);
		}

		[TestMethod]
		public void TestUserData()
		{
			const int userDataValue = 123456789;

			QuadTree<int> tree = new QuadTree<int>(new AABB(new Vector2D(0.0, 0.0), new Vector2D(1.0, 1.0)));
			tree.Insert(new AABB(new Vector2D(0.1, 0.1), new Vector2D(0.9, 0.9)), userDataValue);

			foreach (QuadTreeEntity<int> entity in tree.GetEntityIterator())
			{
				Assert.IsTrue(entity.GetUserData() == userDataValue);
			}
		}
	}
}
