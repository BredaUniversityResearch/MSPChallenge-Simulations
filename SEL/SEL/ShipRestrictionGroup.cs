//using SEL.API;
//using System;
//using System.Collections.Generic;

//namespace SEL
//{
//	/// <summary>
//	/// Restriction group which applies to a number of ships.
//	/// This specifies what ship types are allowed on a certain lane.
//	/// </summary>
//	public class ShipRestrictionGroup
//	{
//		public readonly int m_restrictionGroupId;
//		public readonly string m_restrictionName;

//		public ShipRestrictionGroup(ShipTypeManager manager, APIShipRestrictionGroup apiData)
//		{
//			m_restrictionGroupId = apiData.group_id;
//			m_restrictionName = apiData.name;
//		}

//		public int GetGroupMask()
//		{
//			return 1 << m_restrictionGroupId;
//		}
//	}
//}
