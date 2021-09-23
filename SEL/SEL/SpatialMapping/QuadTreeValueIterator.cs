using System;
using System.Collections;
using System.Collections.Generic;

namespace SEL.SpatialMapping
{
	public class QuadTreeValueIterator<DATA_TYPE> : IEnumerable<DATA_TYPE>
	{
		private QuadTreeNode<DATA_TYPE> m_rootNode;
		private IQuadTreeNodeSelector m_selector = null;

		public QuadTreeValueIterator(QuadTreeNode<DATA_TYPE> root, IQuadTreeNodeSelector selector = null)
		{
			m_rootNode = root;
			m_selector = selector;
		}

		public IEnumerator<DATA_TYPE> GetEnumerator()
		{
			return new QuadTreeValueEnumerator<DATA_TYPE>(m_rootNode, m_selector);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new QuadTreeValueEnumerator<DATA_TYPE>(m_rootNode, m_selector);
		}
	}

	public class QuadTreeValueEnumerator<DATA_TYPE> : IEnumerator<DATA_TYPE>
	{
		private QuadTreeEntityEnumerator<DATA_TYPE> m_internalEnumerator;

		public QuadTreeValueEnumerator(QuadTreeNode<DATA_TYPE> root, IQuadTreeNodeSelector selector)
		{
			m_internalEnumerator = new QuadTreeEntityEnumerator<DATA_TYPE>(root, selector);
		}

		public DATA_TYPE Current
		{
			get
			{
				return m_internalEnumerator.Current.GetUserData();
			}
		}

		object IEnumerator.Current
		{
			get
			{
				return m_internalEnumerator.Current.GetUserData();
			}
		}

		public void Dispose()
		{
			m_internalEnumerator.Dispose();
		}

		public bool MoveNext()
		{
			return m_internalEnumerator.MoveNext();
		}

		public void Reset()
		{
			m_internalEnumerator.Reset();
		}
	}
}
