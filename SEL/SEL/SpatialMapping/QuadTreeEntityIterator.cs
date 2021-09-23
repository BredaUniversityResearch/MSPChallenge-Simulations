using System.Collections;
using System.Collections.Generic;

namespace SEL.SpatialMapping
{
	public class QuadTreeEntityIterator<DATA_TYPE> : IEnumerable<QuadTreeEntity<DATA_TYPE>>
	{
		private QuadTreeNode<DATA_TYPE> m_rootNode = null;
		private IQuadTreeNodeSelector m_nodeSelector = null;

		public QuadTreeEntityIterator(QuadTreeNode<DATA_TYPE> root, IQuadTreeNodeSelector selector = null)
		{
			m_rootNode = root;
			m_nodeSelector = selector;
		}

		public IEnumerator<QuadTreeEntity<DATA_TYPE>> GetEnumerator()
		{
			return new QuadTreeEntityEnumerator<DATA_TYPE>(m_rootNode, m_nodeSelector);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new QuadTreeEntityEnumerator<DATA_TYPE>(m_rootNode, m_nodeSelector);
		}
	}

	public class QuadTreeEntityEnumerator<DATA_TYPE> : IEnumerator<QuadTreeEntity<DATA_TYPE>>
	{
		private QuadTreeNodeEnumerator<DATA_TYPE> m_nodeEnumerator;
		private QuadTreeNode<DATA_TYPE> m_currentNode = null; //The node we are currently iterating.
		private int m_currentNodeEntityIndex = 0;

		public QuadTreeEntity<DATA_TYPE> Current
		{
			get
			{
				return m_currentNode.GetContainedEntity(m_currentNodeEntityIndex);
			}
		}

		object IEnumerator.Current
		{
			get
			{
				return m_currentNode.GetContainedEntity(m_currentNodeEntityIndex);
			}
		}

		public QuadTreeEntityEnumerator(QuadTreeNode<DATA_TYPE> rootNode, IQuadTreeNodeSelector selector)
		{
			m_nodeEnumerator = new QuadTreeNodeEnumerator<DATA_TYPE>(rootNode, selector);
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			if (m_currentNode != null)
			{
				++m_currentNodeEntityIndex;
				if (m_currentNodeEntityIndex >= m_currentNode.GetContainedEntityCount())
				{
					//Move to the next node.
					m_currentNode = null;
				}
			}

			while (m_currentNode == null && m_nodeEnumerator.MoveNext())
			{
				if (m_nodeEnumerator.Current.HasContainedEntities())
				{
					m_currentNode = m_nodeEnumerator.Current;
					m_currentNodeEntityIndex = 0;
					break;
				}
			}
			return m_currentNode != null;
		}

		public void Reset()
		{
			m_currentNode = null;
			m_currentNodeEntityIndex = 0;
		}
	}
}
