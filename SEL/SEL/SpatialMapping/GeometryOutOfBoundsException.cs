using System;

namespace SEL.SpatialMapping
{
	public class GeometryOutOfBoundsException : Exception
	{
		public GeometryOutOfBoundsException(string message)
			: base(message)
		{
		}
	};
}