using System;
using System.Collections.Generic;

namespace SEL.SpatialMapping
{
	public class QuadTreeNode<DATA_TYPE>: IQuadTreeNode
	{
		private const int SUBDIVIDE_COUNT = 8;

		public AABB bounds { get; private set; }
		private QuadTreeNode<DATA_TYPE>[] m_children = null;
		private List<QuadTreeEntity<DATA_TYPE>> m_containedEntities = new List<QuadTreeEntity<DATA_TYPE>>();

		public QuadTreeNode(AABB a_bounds)
		{
			bounds = a_bounds;
		}

		public QuadTreeNode(Vector2D a_min, Vector2D a_max)
		{
			bounds = new AABB(a_min, a_max);
		}

		public void Insert(QuadTreeEntity<DATA_TYPE> entity)
		{
			bool insertedInChild = false;
			if (HasChildren())
			{
				foreach (QuadTreeNode<DATA_TYPE> child in m_children)
				{
					if (child.bounds.IntersectTest(entity.bounds) == EIntersectResult.Contained)
					{
						child.Insert(entity);
						insertedInChild = true;
						break;
					}
				}
			}

			if (!insertedInChild)
			{
				m_containedEntities.Add(entity);
				if (m_children == null && m_containedEntities.Count >= SUBDIVIDE_COUNT)
				{
					Subdivide();
					ReinsertChildrenToChildNodes();
				}
			}
		}

		public IEnumerable<QuadTreeNode<DATA_TYPE>> GetChildNodes()
		{
			return m_children;
		}

		public bool HasChildren()
		{
			return m_children != null;
		}

		public int GetPopulatedChildNodeCount()
		{
			int count = 0;
			if (HasChildren())
			{
				foreach (QuadTreeNode<DATA_TYPE> childNode in m_children)
				{
					count += childNode.GetPopulatedChildNodeCount();
				}
			}

			if (m_containedEntities.Count > 0)
			{
				++count;
			}
			return count;
		}

		private void Subdivide()
		{
			m_children = new QuadTreeNode<DATA_TYPE>[4];
			Vector2D subdividedSize = (bounds.max - bounds.min) * 0.5;

			Vector2D[] childOffsets =
			{
				new Vector2D(0.0, 0.0),
				new Vector2D(subdividedSize.x, 0.0),
				new Vector2D(0.0, subdividedSize.x),
				new Vector2D(subdividedSize.x, subdividedSize.y)
			};

			for (int i = 0; i < m_children.Length; ++i)
			{
				m_children[i] = new QuadTreeNode<DATA_TYPE>(bounds.min + childOffsets[i], bounds.min + childOffsets[i] + subdividedSize);
			}
		}

		private void ReinsertChildrenToChildNodes()
		{
			QuadTreeEntity<DATA_TYPE>[] containedEntities = m_containedEntities.ToArray();
			m_containedEntities.Clear();
			
			foreach(QuadTreeEntity<DATA_TYPE> containedEntity in containedEntities)
			{
				Insert(containedEntity);
			}
		}

		public bool HasContainedEntities()
		{
			return m_containedEntities.Count > 0;
		}

		internal QuadTreeEntity<DATA_TYPE> GetContainedEntity(int entityIndex)
		{
			return m_containedEntities[entityIndex];
		}

		public int GetContainedEntityCount()
		{
			return m_containedEntities.Count;
		}
	}
}
