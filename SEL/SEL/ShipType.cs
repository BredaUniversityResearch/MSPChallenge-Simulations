using SEL.API;
using System;
using SEL.Routing;

namespace SEL
{
	public class ShipType
	{
		public byte ShipTypeId { get; }
		public string ShipTypeName { get; }
		public float ShipAgilityValue { get; }
		public EShipRoutingType ShipRoutingType { get; }
		private APIShipType m_debugApiShipType;

		public ShipType(APIShipType apiShipType, ShipTypeManager shipTypeManager)
		{
			if (apiShipType.ship_type_id > 255)
				throw new ArgumentOutOfRangeException(string.Format("ship_type_id is limited to 255 values. Found ship type id {0}", apiShipType.ship_type_id));
			ShipTypeId = (byte)apiShipType.ship_type_id;
			ShipTypeName = apiShipType.ship_type_name;
			ShipAgilityValue = apiShipType.ship_agility;
			ShipRoutingType = apiShipType.ship_routing_type;
			m_debugApiShipType = apiShipType;
		}

		public string GetDebugInfo()
		{
			return string.Format("ID: {0} TypeName: {1} AgilityType: {2}", m_debugApiShipType.ship_type_id, m_debugApiShipType.ship_type_name, ShipAgilityValue);
		}
	}
}
