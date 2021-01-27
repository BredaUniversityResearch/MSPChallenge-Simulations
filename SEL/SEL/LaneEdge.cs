using System;

namespace SEL
{
	/// <summary>
	/// edge for the lane graph
	/// </summary>
	class LaneEdge : GeometryEdgeTyped<LaneVertex>
	{
		public const float LaneWidthNotDefined = -1.0f;

		public readonly ELaneEdgeType m_laneType;
		private RestrictionGeometryType m_restrictionType = null;
		private float m_restrictionOverlapAmount = 0.0f;
		private readonly EEdgeDirectionality m_directionality;
		public readonly float m_laneWidth; //Currently only used for passing through to SAMSON/Marin simulation. Does not affect SEL simulation at all.

		public LaneEdge(LaneVertex from, LaneVertex to, ELaneEdgeType laneType, EEdgeDirectionality directionality, float laneWidth)
			: base(from, to)
		{
			m_laneType = laneType;
			m_directionality = directionality;
			m_laneWidth = laneWidth;
		}

		public void SetRestrictionType(RestrictionGeometryType restrictionType, float restrictionOverlapAmount)
		{
			m_restrictionType = restrictionType;
			m_restrictionOverlapAmount = restrictionOverlapAmount;
		}

		public bool IsShipTypeAllowed(int shipTypeId)
		{
			return (m_restrictionType == null || (m_restrictionType.GetAllowedShipTypeMask() & (1 << shipTypeId)) != 0);
		}

		public bool IsUnidirectional()
		{
			return m_directionality == EEdgeDirectionality.Unidirectional;
		}

		public bool IsTraversableFrom(LaneVertex vertex)
		{
			bool result = !(IsUnidirectional() && m_from != vertex);
			return result;
		}

		public double GetTravelCostMultiplier(int shipTypeId)
		{
			if (m_restrictionType != null)
			{
				float shipCostMultiplier = m_restrictionType.GetShipCostMultiplier(shipTypeId);
				if (shipCostMultiplier > 1.0f)
				{
					return Math.Max(1.0f, shipCostMultiplier * m_restrictionOverlapAmount);
				}
			}

			return 1.0f;
		}

		public float GetRestrictionOverlapAmount()
		{
			return m_restrictionOverlapAmount;
		}

		public void SetAllowedShipTypes(ShipType[] allowedShipTypes)
		{
			int[] shipTypeIds = new int[allowedShipTypes.Length];
			for (int i = 0; i < shipTypeIds.Length; ++i)
			{
				shipTypeIds[i] = allowedShipTypes[i].ShipTypeId;
			}

			m_restrictionType = new RestrictionGeometryType(shipTypeIds, 1.0f);
		}
	}
}
