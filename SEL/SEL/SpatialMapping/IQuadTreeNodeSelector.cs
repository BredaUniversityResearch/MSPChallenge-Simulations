namespace SEL.SpatialMapping
{
	/// <summary>
	/// Interface for a node selector which approves or rejects nodes depending on some logic
	/// </summary>
	public interface IQuadTreeNodeSelector
	{
		bool EvaluateNode(IQuadTreeNode node);
	}
}
