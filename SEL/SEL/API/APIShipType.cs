using SEL.Routing;

namespace SEL.API
{
	public class APIShipType
	{
		public int ship_type_id;				//ID of this ship type combination.
		public string ship_type_name;			//Name of this ship type combination (Passenger S, Passenger L, Ferry, etc.)
		public string ship_restriction_group;	//Restriction group this ship type falls in.
		public float ship_agility;				//Agility value, or the willingness to take implicit lanes. Implicit lanes cost is multiplied by the inverse of this value.
		public EShipRoutingType ship_routing_type = EShipRoutingType.RegularShipping; //Defines what kind of routes should we generate for this ship
	}
}
