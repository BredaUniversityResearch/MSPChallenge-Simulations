namespace SELRELBridge.API
{
	public class SELOutputData
	{
		public const int MessageIdentifier = 1;

		public int m_simulatedMonth = 0;
		public APIRouteGraphEdge[] m_routeGraphEdges = null;
		public APIRouteGraphVertex[] m_routeGraphPoints = null;
		public APIRouteGraphEdgeIntensity[] m_routeGraphIntensities = null;
	}
}
