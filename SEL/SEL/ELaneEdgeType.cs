namespace SEL
{
	public enum ELaneEdgeType
	{
		Persistent, //Persistent lane defined by data coming from the simulation e.i. predefined or planned lanes
		Implicit,   //Implicitly generated lanes for the simulation to allow ships to go from lane A to B or anywhere they would like.
		Merge,      //Vertices that occupy almost the same location are connected via an edge of this type.
	}
}
