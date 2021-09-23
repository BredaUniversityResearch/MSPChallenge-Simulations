namespace SEL.SpatialMapping
{
	/// <summary>
	/// Interface to the QuadTreeNode class which exposes only the basic functionality so we don't need to pass in the correct generic types to where we don't need it.
	/// </summary>
	public interface IQuadTreeNode
	{
		AABB bounds { get; }
	}
}
