using System;
using System.Collections;
using System.Collections.Generic;

namespace SEL.SpatialMapping
{
	public class QuadTreeSpatialQueryResult<DATA_TYPE>: IEnumerable<DATA_TYPE>
	{
		private List<DATA_TYPE> m_foundEntities = new List<DATA_TYPE>();

		public void AddResult(QuadTreeEntity<DATA_TYPE> entity)
		{
			//Maybe at some point in the future we need to store the entire entity but currently I don't see the point of it...
			m_foundEntities.Add(entity.GetUserData());
		}

		public int GetResultCount()
		{
			return m_foundEntities.Count;
		}

		public IEnumerator<DATA_TYPE> GetEnumerator()
		{
			return m_foundEntities.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_foundEntities.GetEnumerator();
		}
	}
}
