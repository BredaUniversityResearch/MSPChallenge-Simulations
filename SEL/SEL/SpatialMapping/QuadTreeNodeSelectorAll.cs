namespace SEL.SpatialMapping
{
	class QuadTreeNodeSelectorAll : IQuadTreeNodeSelector
	{
		public static readonly QuadTreeNodeSelectorAll Instance = new QuadTreeNodeSelectorAll();

		public bool EvaluateNode(IQuadTreeNode node)
		{
			return true;
		}
	}
}
