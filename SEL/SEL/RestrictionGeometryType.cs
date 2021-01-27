using System;
using System.Collections.Generic;
using System.Linq;

namespace SEL
{
	/// <summary>
	/// Restriction type containing more information for the restriction geometry such as what ships are allowed to pass.
	/// </summary>
	public class RestrictionGeometryType
	{
		public class EqualityComparer : IEqualityComparer<RestrictionGeometryType>
		{
			public static readonly EqualityComparer Instance = new EqualityComparer();

			public bool Equals(RestrictionGeometryType x, RestrictionGeometryType y)
			{
				if (x == null || y == null)
				{
					return false;
				}

				if (ReferenceEquals(x, y))
				{
					return true;
				}

				return x.m_allowedShipTypeMask == y.m_allowedShipTypeMask &&
					   x.m_allowedShipTypeCostMultiplier.SequenceEqual(y.m_allowedShipTypeCostMultiplier);
			}

			public int GetHashCode(RestrictionGeometryType obj)
			{
				return obj.m_allowedShipTypeMask;
			}
		}

		public static readonly RestrictionGeometryType DisallowAll = new RestrictionGeometryType(new int[0] { }, 1.0f);

		public static readonly RestrictionGeometryType AllowAll = new RestrictionGeometryType(new int[31]
			{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 }, 1.0f); 

		private readonly int[] m_allowedShipTypeIds;
		private readonly int m_allowedShipTypeMask; // mask & (1 << shipTypeId) != 0 defines what is allowed.

		private readonly float[] m_allowedShipTypeCostMultiplier;

		public RestrictionGeometryType(int[] allowedShipTypeIds, float allowedShipsCostMultiplier)
		{
			float[] costMultiplierPerRestrictionType = new float[allowedShipTypeIds.Length];
			for (int i = 0; i < costMultiplierPerRestrictionType.Length; ++i)
			{
				costMultiplierPerRestrictionType[i] = allowedShipsCostMultiplier;
			}

			m_allowedShipTypeIds = allowedShipTypeIds;
			m_allowedShipTypeCostMultiplier = costMultiplierPerRestrictionType;
			m_allowedShipTypeMask = CreateAllowedShipTypeMask(m_allowedShipTypeIds);
		}

		public RestrictionGeometryType(int[] allowedShipTypeIds, float[] allowedShipTypeCostMultiplier)
		{
			if (allowedShipTypeIds.Length != allowedShipTypeCostMultiplier.Length)
			{
				throw new ArgumentException("allowedShipRestrictionGroups.Length != allowedShipsCostMultiplier.Length");
			}

			m_allowedShipTypeIds = allowedShipTypeIds;
			m_allowedShipTypeCostMultiplier = allowedShipTypeCostMultiplier;
			m_allowedShipTypeMask = CreateAllowedShipTypeMask(m_allowedShipTypeIds);
		}

		public int GetAllowedShipTypeMask()
		{
			return m_allowedShipTypeMask;
		}

		private static int CreateAllowedShipTypeMask(IEnumerable<int> allowedShipTypeIds)
		{
			int mask = 0;
			foreach (int allowedShipRestrictionGroup in allowedShipTypeIds)
			{
				mask |= (1 << allowedShipRestrictionGroup);
			}
			return mask;
		}

		public float GetShipCostMultiplier(int shipTypeId)
		{
			float multiplier = 0.0f;
			for (int i = 0; i < m_allowedShipTypeIds.Length; ++i)
			{
				if (m_allowedShipTypeIds[i] == shipTypeId)
				{
					multiplier = m_allowedShipTypeCostMultiplier[i];
					break;
				}
			}

			if (multiplier == 0.0f)
			{
				throw new Exception(
					$"Could not find AllowedShipRestrictionGroup for type id {shipTypeId} on restriction geometry type.");
			}

			return multiplier;
		}

		public bool HasAnyDefinedCostMultiplier()
		{
			for(int i = 0; i < m_allowedShipTypeCostMultiplier.Length; ++i)
			{
				if (m_allowedShipTypeCostMultiplier[i] != 1.0f)
				{
					return true;
				}
			}

			return false;
		}

		public static RestrictionGeometryType CreateCompoundType(ICollection<RestrictionGeometryType> restrictionGeometryTypes)
		{
			if (restrictionGeometryTypes.Count == 0)
			{
				return DisallowAll;
			}

			if (restrictionGeometryTypes.Count == 1)
			{
				foreach (RestrictionGeometryType type in restrictionGeometryTypes)
				{
					//Yeah... Avoid creating new types and use the only one that we need.
					return type;
				}
			}

			//Create a compound type by using an intersection of all types. 
			bool isSettingUpBaseSet = true;
			Dictionary<int, float> typeMultiplier = new Dictionary<int, float>();
			foreach (RestrictionGeometryType type in restrictionGeometryTypes)
			{
				List<int> typesToRemove = new List<int>(typeMultiplier.Keys);

				for (int i = 0; i < type.m_allowedShipTypeIds.Length; ++i)
				{
					int restrictionGroupId = type.m_allowedShipTypeIds[i];
					if (typeMultiplier.TryGetValue(restrictionGroupId, out var maxMultiplier) || isSettingUpBaseSet)
					{
						maxMultiplier = Math.Max(maxMultiplier, type.m_allowedShipTypeCostMultiplier[i]);

						typeMultiplier[restrictionGroupId] = maxMultiplier;
						typesToRemove.Remove(restrictionGroupId);
					}
				}

				if (!isSettingUpBaseSet)
				{
					foreach (int typeToRemove in typesToRemove)
					{
						typeMultiplier.Remove(typeToRemove);
					}
				}

				isSettingUpBaseSet = false;
			}

			//Short circuit so we don't create any unnecessary types.
			if (typeMultiplier.Count == 0)
			{
				return DisallowAll;
			}

			int[] allowedShipGroupsArray = new int[typeMultiplier.Count];
			float[] allowedShipCostMultiplierArray = new float[typeMultiplier.Count];
			int entryIndex = 0;
			foreach (KeyValuePair<int, float> kvp in typeMultiplier)
			{
				allowedShipGroupsArray[entryIndex] = kvp.Key;
				allowedShipCostMultiplierArray[entryIndex] = kvp.Value;
				++entryIndex;
			}
			return new RestrictionGeometryType(allowedShipGroupsArray, allowedShipCostMultiplierArray);
		}
	}
}
