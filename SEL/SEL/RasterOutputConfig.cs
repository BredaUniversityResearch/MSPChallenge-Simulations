using SEL.SpatialMapping;

namespace SEL
{
	class RasterOutputConfig
	{
		public readonly AABB m_subBounds;

		public readonly int m_fullResolutionX;
		public readonly int m_fullResolutionY;

		public readonly int m_outputResolutionX;
		public readonly int m_outputResolutionY;

		public RasterOutputConfig(int fullResolutionX, int fullResolutionY, int outputResolutionX = -1, int outputResolutionY = -1, AABB subBounds = new AABB())
		{
			m_fullResolutionX = fullResolutionX;
			m_fullResolutionY = fullResolutionY;
			m_outputResolutionX = outputResolutionX;
			m_outputResolutionY = outputResolutionY;
			m_subBounds = subBounds;
		}
	}
}