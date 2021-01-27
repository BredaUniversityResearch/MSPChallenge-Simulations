
using System.Collections.Generic;

namespace SEL.KPI
{
	/// <summary>
	/// Calculates a shipping intensity per-lane by going over all routes 
	/// </summary>
	class KPIPerLaneGeometryShippingIntensity : KPIBase
	{
		public override void Calculate(KPIInputData data)
		{
			Dictionary<int, int> perGeometryIntensity = new Dictionary<int, int>();

			foreach (RouteIntensity routeIntensity in data.intensityManager.GetAllAbsoluteRouteIntensities(data.month, data.portManager))
			{
				Route foundRoute = data.routeManager.FindCachedRoute(routeIntensity);
				if (foundRoute != null)
				{
					HashSet<int> visitedGeometryIds = new HashSet<int>();
					foreach (LaneEdge edge in foundRoute.GetRouteEdges())
					{
						VisitNodeVertex(perGeometryIntensity, visitedGeometryIds, edge.typedFrom.geometryId, routeIntensity.Intensity);
						if (edge.typedTo.geometryId != edge.typedFrom.geometryId)
						{
							VisitNodeVertex(perGeometryIntensity, visitedGeometryIds, edge.typedTo.geometryId, routeIntensity.Intensity);
						}
					}
				}
			}

			//Submit results.
			kpiManager.GetApiConnector().SetShippingIntensityValues(perGeometryIntensity);
		}

		private static void VisitNodeVertex(Dictionary<int, int> outputIntensites, HashSet<int> visitedNodesThisRoute, int vertexGeometryId, int intensity)
		{
			if (vertexGeometryId != -1) //Ignore implicitly generated lanes.
			{
				if (!visitedNodesThisRoute.Contains(vertexGeometryId))
				{
					int previousIntensity;
					if (!outputIntensites.TryGetValue(vertexGeometryId, out previousIntensity))
					{
						previousIntensity = 0;
					}
					outputIntensites[vertexGeometryId] = previousIntensity + intensity;
				}
			}
		}
	}
}
