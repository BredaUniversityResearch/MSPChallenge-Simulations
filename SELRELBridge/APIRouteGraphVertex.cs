namespace SELRELBridge.API
{
	class APIRouteGraphVertex
	{
		public int vertex_id;
		public double position_x;
		public double position_y;

		public APIRouteGraphVertex(GeometryVertex vertex)
		{
			vertex_id = vertex.vertexId;
			position_x = vertex.position.x;
			position_y = vertex.position.y;
		}
	}
}
