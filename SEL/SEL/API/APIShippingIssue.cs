namespace SEL.API
{
	/// <summary>
	/// Class for submitting shipping issues.
	/// </summary>
	class APIShippingIssue
	{
		public APIShippingIssue(int sourceGeometryPersistentId, int destinationGeometryPersistentId, string message)
		{
			source_geometry_persistent_id = sourceGeometryPersistentId;
			destination_geometry_persistent_id = destinationGeometryPersistentId;
			this.message = message;
		}

		public int source_geometry_persistent_id;
		public int destination_geometry_persistent_id;
		public string message;
	}
}
