namespace SEL
{
	public class GeometryType
	{
		public readonly int LayerId;
		public readonly int LayerType;

		public GeometryType(int a_layerId, int a_layerType)
		{
			LayerId = a_layerId;
			LayerType = a_layerType;
		}

		public override int GetHashCode()
		{
			return LayerId ^ LayerType;
		}
	}
}
