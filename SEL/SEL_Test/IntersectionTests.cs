using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEL;

namespace SEL_Test
{
	[TestClass]
	public class IntersectionTests
	{
		[TestMethod]
		public void LineSegmentsIntersect()
		{
			GeometryEdge edge1 = new GeometryEdge(new GeometryVertex(0, 0), new GeometryVertex(5, 5));
			GeometryEdge edge2 = new GeometryEdge(new GeometryVertex(0, 5), new GeometryVertex(5, 0));

			Assert.IsTrue(edge1.IntersectionTest(edge2, out float intersectionTime));
		}

		[TestMethod]
		public void LineSegmentsDoNotIntersect()
		{
			GeometryEdge edge1 = new GeometryEdge(new GeometryVertex(3, 0), new GeometryVertex(3, 4));
			GeometryEdge edge2 = new GeometryEdge(new GeometryVertex(0, 5), new GeometryVertex(5, 5));

			Assert.IsFalse(edge1.IntersectionTest(edge2, out float intersectionTime));
		}

		[TestMethod]
		public void LineSegmentsAreCollinearAndOverlapping()
		{
			GeometryEdge edge1 = new GeometryEdge(new GeometryVertex(0, 0), new GeometryVertex(2, 0));
			GeometryEdge edge2 = new GeometryEdge(new GeometryVertex(1, 0), new GeometryVertex(3, 0));

			Assert.IsTrue(edge1.IntersectionTest(edge2, out float intersectionTime));
		}
	}
}
