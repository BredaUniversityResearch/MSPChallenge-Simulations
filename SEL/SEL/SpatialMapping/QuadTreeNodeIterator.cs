using System;
using System.Collections;
using System.Collections.Generic;

namespace SEL.SpatialMapping
{
	public class QuadTreeNodeIterator<DATA_TYPE> : IEnumerable<QuadTreeNode<DATA_TYPE>>
	{
		private QuadTreeNode<DATA_TYPE> m_rootNode = null;
		private IQuadTreeNodeSelector m_nodeSelector = null;

		public QuadTreeNodeIterator(QuadTreeNode<DATA_TYPE> root, IQuadTreeNodeSelector selector = null)
		{
			m_rootNode = root;
			m_nodeSelector = selector;
		}

		public IEnumerator<QuadTreeNode<DATA_TYPE>> GetEnumerator()
		{
			return new QuadTreeNodeEnumerator<DATA_TYPE>(m_rootNode, m_nodeSelector);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new QuadTreeNodeEnumerator<DATA_TYPE>(m_rootNode, m_nodeSelector);
		}
	}

	public class QuadTreeNodeEnumerator<DATA_TYPE> : IEnumerator<QuadTreeNode<DATA_TYPE>>
	{
		private Queue<QuadTreeNode<DATA_TYPE>> m_nodeQueue = new Queue<QuadTreeNode<DATA_TYPE>>(); //Nodes that we still need to iterate.
		private IQuadTreeNodeSelector m_nodeSelector = null;
		private QuadTreeNode<DATA_TYPE> m_currentNode = null;

		public QuadTreeNode<DATA_TYPE> Current
		{
			get
			{
				return m_currentNode;
			}
		}

		object IEnumerator.Current
		{
			get
			{
				return m_currentNode;
			}
		}

		public QuadTreeNodeEnumerator(QuadTreeNode<DATA_TYPE> rootNode, IQuadTreeNodeSelector selector)
		{
			if (rootNode == null)
				throw new ArgumentNullException();
			
			m_nodeQueue.Enqueue(rootNode);
			m_nodeSelector = (selector != null) ? selector : QuadTreeNodeSelectorAll.Instance;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			if (m_nodeQueue.Count > 0)
			{
				m_currentNode = m_nodeQueue.Dequeue();
				if (m_currentNode.HasChildren())
				{
					foreach (QuadTreeNode<DATA_TYPE> childNode in m_currentNode.GetChildNodes())
					{
						if (m_nodeSelector.EvaluateNode(childNode))
						{
							m_nodeQueue.Enqueue(childNode);
						}
					}
				}
			}
			else
			{
				m_currentNode = null;
			}

			return m_currentNode != null;
		}

		public void Reset()
		{
			m_nodeQueue.Clear();
			m_currentNode = null;
		}
	}
}
