using System;
using System.Diagnostics.CodeAnalysis;

namespace SELRELBridge.API
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class APIGeometryType : IEquatable<APIGeometryType>
	{
		public int layer_id;
		public int layer_type;

		public APIGeometryType(int a_layerId, int a_layerType)
		{
			layer_id = a_layerId;
			layer_type = a_layerType;
		}

		public bool Equals(APIGeometryType other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return layer_id == other.layer_id && layer_type == other.layer_type;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((APIGeometryType) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (layer_id * 397) ^ layer_type;
			}
		}

		public static bool operator ==(APIGeometryType left, APIGeometryType right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(APIGeometryType left, APIGeometryType right)
		{
			return !Equals(left, right);
		}
	}
}
