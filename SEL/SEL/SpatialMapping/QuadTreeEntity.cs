namespace SEL.SpatialMapping
{
	public class QuadTreeEntity<DATA_TYPE>
	{
		public AABB bounds { get; private set; }
		private DATA_TYPE m_userData;

		public QuadTreeEntity(AABB entityBounds, DATA_TYPE userData)
		{
			bounds = entityBounds;
			m_userData = userData;
		}

		public DATA_TYPE GetUserData()
		{
			return m_userData;
		}
	}
}
