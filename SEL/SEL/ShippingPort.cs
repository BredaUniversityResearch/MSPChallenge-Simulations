using SEL.API;
using System;
using System.Collections.Generic;

namespace SEL
{
	/// <summary>
	/// Producer / Consumer of ships
	/// </summary>
	class ShippingPort
	{
		private List<ShipType> m_acceptingShipTypes = new List<ShipType>();
		private int m_acceptingShipTypeIdMask = 0;

		public Vector2D Center { get; private set; }
		public string PortName { get; private set; }
		private APIShippingPortGeometry Geometry { get; set; }
		public EShippingPortType ShippingPortType { get; private set; }
		public LaneVertex PortPathingVertex { get; private set; }

		public int OwningCountryId { get; private set; }
		public int ConstructionStartTime { get; private set; }
		public int ConstructionEndTime { get; private set; }
		public int GeometryPersistentId { get; private set; }

		public int GeometryPoints => Geometry.geometry.Length;

		public ShippingPort(double x, double y, string portName, APIShippingPortGeometry originalGeometry, EShippingPortType portType)
		{
			Center = new Vector2D(x, y);
			PortName = portName;
			Geometry = originalGeometry;
			GeometryPersistentId = originalGeometry.geometry_persistent_id;
			ConstructionStartTime = originalGeometry.construction_start_time;
			ConstructionEndTime = originalGeometry.construction_end_time;
			ShippingPortType = portType;
			OwningCountryId = -1;
		}

		public void SetPathingVertex(LaneVertex vertex)
		{
			PortPathingVertex = vertex;
		}

		public void SetAcceptsShipType(ShipType shipType)
		{
			if (!IsAcceptingShipType(shipType))
			{
				m_acceptingShipTypes.Add(shipType);
				if (shipType.ShipTypeId > 31)
					throw new ArgumentException("Need to grow bitmask here.");
				m_acceptingShipTypeIdMask |= (1 << shipType.ShipTypeId);
			}
		}

		public bool IsAcceptingShipType(ShipType shipType)
		{
			if (shipType.ShipTypeId > 31)
				throw new ArgumentException(string.Format("Need to grow bitmask here. ShipTypeId is out of bounds. {0} out of a maximum of 31", shipType.ShipTypeId));
			return (m_acceptingShipTypeIdMask & (1 << shipType.ShipTypeId)) != 0;
		}

		public IEnumerable<ShipType> GetAcceptingShipTypes()
		{
			return m_acceptingShipTypes;
		}

		public double GetSurfaceArea()
		{
			return GetPolygonArea(Geometry.geometry);
		}

		private static double GetPolygonArea(double[][] polygon)
		{
			double area = 0;
			for (int i = 0; i < polygon.Length; ++i)
			{
				int j = (i + 1) % polygon.Length;
				area += polygon[i][1] * polygon[j][0] - polygon[i][0] * polygon[j][1];
			}
			return Math.Abs(area * 0.5f);
		}
	}
}
