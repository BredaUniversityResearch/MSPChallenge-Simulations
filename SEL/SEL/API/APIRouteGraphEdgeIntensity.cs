namespace SEL.API
{
	class APIRouteGraphEdgeIntensity
	{
		public readonly int edge_id;
		public readonly int ship_type_id;
		public int intensity;

		public APIRouteGraphEdgeIntensity(int edgeId, int shipTypeId, int intensity)
		{
			edge_id = edgeId;
			ship_type_id = shipTypeId;
			this.intensity = intensity;
		}
	}
}
