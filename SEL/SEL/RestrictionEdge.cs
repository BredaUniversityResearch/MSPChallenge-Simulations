using System;
using SEL.SpatialMapping;
using System.Collections.Generic;

namespace SEL
{
	/// <summary>
	/// An edge structure defining a line where certain restrictions apply.
	/// Crossing one of these restriction edges will impose the restrictions onto the edge that crosses it.
	/// When a restriction is a no-go zone for shipping any edges that intersect with this will be discarded.
	/// otherwise the capabilities of the edge are modified depending on the restriction configuration.
	/// </summary>
	class RestrictionEdge : GeometryEdgeTyped<GeometryVertex>
	{
		public readonly RestrictionGeometryType m_geometryType;
		public readonly RestrictionMesh m_parentMesh;

		public RestrictionEdge(GeometryVertex from, GeometryVertex to, RestrictionGeometryType geometryType, RestrictionMesh parentMesh) 
			: base(from, to)
		{
			m_geometryType = geometryType;
			m_parentMesh = parentMesh;
		}

		public RestrictionGeometryType GetRestrictionType()
		{
			return m_geometryType;
		}
	}
}
