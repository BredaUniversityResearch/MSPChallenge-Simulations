using SEL.API;
using System.Collections.Generic;
using System;

namespace SEL
{
	/// <summary>
	/// Class for holding all ship types.
	/// </summary>
	public class ShipTypeManager
	{
		private List<ShipType> m_availableShipTypes = new List<ShipType>();

		public void ImportShipTypes(APIShipType[] shipTypes)
		{
			foreach (APIShipType type in shipTypes)
			{
				if (type.ship_type_id > 0)
				{
					m_availableShipTypes.Add(new ShipType(type, this));
				}
				else
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						$"ShipType ({type.ship_type_name}) with id <= 0({type.ship_type_id}) found. Ship Type id's should be positive numbers starting from 1.");
				}
			}
		}

		public ShipType FindShipTypeById(int shipTypeId)
		{
			ShipType result = null;
			foreach(ShipType shipType in m_availableShipTypes)
			{
				if (shipType.ShipTypeId == shipTypeId)
				{
					result = shipType;
				}
			}
			return result;
		}

		public IEnumerable<ShipType> GetShipTypes()
		{
			return m_availableShipTypes;
		}
	}
}
