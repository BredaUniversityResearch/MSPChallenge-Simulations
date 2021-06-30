using System.Collections.Generic;

namespace SEL.SpatialMapping
{
	public class QuadTree<DATA_TYPE>
	{
		private QuadTreeNode<DATA_TYPE> m_root = null;

		public QuadTree(AABB initialBounds)
		{
			m_root = new QuadTreeNode<DATA_TYPE>(initialBounds);
		}

		public void Insert(AABB bounds, DATA_TYPE userData)
		{
			QuadTreeEntity<DATA_TYPE> entity = new QuadTreeEntity<DATA_TYPE>(bounds, userData);
			if (m_root.bounds.IntersectTest(entity.bounds) != EIntersectResult.NoIntersection)
			{
				m_root.Insert(entity);
			}
			else
			{
				throw new GeometryOutOfBoundsException("Found geometry outside of the playable area, this is not supported. Please check that all geometry that is used in SEL is contained inside the playable area.");
			}
		}

		/// <summary>
		/// Queries the given AABB and returns a SpatialQueryResult with all the entities overlapping these bounds.
		/// </summary>
		/// <param name="queryParams">Parameters for the query.</param>
		/// <returns></returns>
		public QuadTreeSpatialQueryResult<DATA_TYPE> Query(SpatialQueryAABB queryParams)
		{
			QuadTreeSpatialQueryResult<DATA_TYPE> result = new QuadTreeSpatialQueryResult<DATA_TYPE>();
			foreach (QuadTreeEntity<DATA_TYPE> entity in GetEntityIterator(queryParams.GetNodeSelector()))
			{
				if (queryParams.CheckBounds(entity.bounds))
				{
					result.AddResult(entity);
				}
			}
			return result;
		}

		public int GetPopulatedNodeCount()
		{
			return m_root.GetPopulatedChildNodeCount();
		}

		internal IEnumerable<DATA_TYPE> GetValueIterator(IQuadTreeNodeSelector selector = null)
		{
			return new QuadTreeValueIterator<DATA_TYPE>(m_root, selector);
		}

		public QuadTreeEntityIterator<DATA_TYPE> GetEntityIterator(IQuadTreeNodeSelector selector = null)
		{
			return new QuadTreeEntityIterator<DATA_TYPE>(m_root, selector);
		}

		public AABB GetRootBounds()
		{
			return m_root.bounds;
		}
	}
}
