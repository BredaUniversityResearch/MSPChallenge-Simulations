namespace SEL
{
	/// <summary>
	/// Typed utility-base class for an Edge type.
	/// </summary>
	/// <typeparam name="VERTEX_TYPE">The type of vertex to be used.</typeparam>
	class GeometryEdgeTyped<VERTEX_TYPE> : GeometryEdge
		where VERTEX_TYPE : GeometryVertex
	{
		public VERTEX_TYPE typedFrom { get { return (VERTEX_TYPE)m_from; } }
		public VERTEX_TYPE typedTo { get { return (VERTEX_TYPE)m_to; } }

		public GeometryEdgeTyped(GeometryVertex from, GeometryVertex to) 
			: base(from, to)
		{
		}
	}
}
