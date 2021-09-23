namespace SEL.API
{
	/// <summary>
	/// API data that specifies what the current update should do.
	/// </summary>
	class APIUpdateDescriptor
	{
		public bool rebuild_edges = false; //Should we re-get and rebuild the shipping / restriction geometry.
	}
}
