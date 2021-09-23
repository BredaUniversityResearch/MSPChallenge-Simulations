using System;
using SEL.API;
using System.Collections.Generic;

namespace SEL
{
	class RestrictionGeometryTypeManager
	{
		private Dictionary<ulong, RestrictionGeometryType> m_restrictionTypes = new Dictionary<ulong, RestrictionGeometryType>();

		public void ImportGeometryTypes(APIRestrictionTypeException[] restrictionTypes)
		{
			foreach (APIRestrictionTypeException restrictionType in restrictionTypes)
			{
				ulong hash = CreateTypeHash(restrictionType.layer_id, restrictionType.layer_type_id);

				RestrictionGeometryType newGeometryType;
				if (restrictionType.cost_multipliers != null)
				{
					newGeometryType = new RestrictionGeometryType(restrictionType.allowed_ship_type_ids,
						restrictionType.cost_multipliers, new []{ new GeometryType(restrictionType.layer_id, restrictionType.layer_type_id) });
				}
				else
				{
					newGeometryType = new RestrictionGeometryType(restrictionType.allowed_ship_type_ids, 1.0f, new []{ new GeometryType(restrictionType.layer_id, restrictionType.layer_type_id) });
				}

				if (m_restrictionTypes.TryGetValue(hash, out var geomType))
				{
					RestrictionGeometryType compoundType =
						RestrictionGeometryType.CreateCompoundType(new List<RestrictionGeometryType>() {geomType, newGeometryType});
					m_restrictionTypes[hash] = compoundType;
				}
				else
				{
					m_restrictionTypes.Add(hash, newGeometryType);
				}
			}
		}

		public RestrictionGeometryType GetAllowedShipMask(int layerId, int[] layerTypes)
		{
			HashSet<RestrictionGeometryType> restrictionGeometryTypes = new HashSet<RestrictionGeometryType>(RestrictionGeometryType.EqualityComparer.Instance);
			if (m_restrictionTypes.TryGetValue(CreateTypeHash(layerId, -1), out var allLayerTypesResult))
			{
				restrictionGeometryTypes.Add(allLayerTypesResult);
			}

			foreach (int layerType in layerTypes)
			{
				ulong layerTypeHash = CreateTypeHash(layerId, layerType);
				m_restrictionTypes.TryGetValue(layerTypeHash, out var layerTypeResult);
				if (layerTypeResult != null)
				{
					restrictionGeometryTypes.Add(layerTypeResult);
				}
			}

			RestrictionGeometryType result;
			if (restrictionGeometryTypes.Count > 0)
			{
				result = RestrictionGeometryType.CreateCompoundType(restrictionGeometryTypes);
			}
			else
			{
				result = RestrictionGeometryType.DisallowAll;
			}
			return result;
		}

		private static ulong CreateTypeHash(int layerId, int layerTypeId)
		{
			ulong layerIdMask = (ulong) ((long) layerId << 32)	& 0xffffffff00000000L;
			ulong layerTypeMask = (ulong) layerTypeId			& 0x00000000ffffffffL;

			return layerIdMask | layerTypeMask;
		}
	}
}