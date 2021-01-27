using System.Diagnostics.CodeAnalysis;

namespace SELRELBridge.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class APIRouteGraphVertex
	{
		public int vertex_id;
		public double position_x;
		public double position_y;

		public APIRouteGraphVertex(int a_vertexId, double a_positionX, double a_positionY)
		{
			vertex_id = a_vertexId;
			position_x = a_positionX;
			position_y = a_positionY;
		}
	}
}
