using System;
using System.Collections.Generic;
using System.Linq;
using SELRELBridge.API;

namespace REL
{
	public class RouteSimplifier
	{
		public readonly APIRouteGraphVertex[] SimplifiedVertices;
		public readonly APIRouteGraphEdge[] SimplifiedEdges;
		public readonly APIRouteGraphEdgeIntensity[] SimplifiedIntensities;

		public RouteSimplifier(APIRouteGraphVertex[] a_vertices, APIRouteGraphEdge[] a_edges, APIRouteGraphEdgeIntensity[] a_intensities)
		{
			HashSet<APIRouteGraphVertex> outputVertices = new HashSet<APIRouteGraphVertex>(a_vertices);
			HashSet<APIRouteGraphEdge> outputEdges = new HashSet<APIRouteGraphEdge>(a_edges);
			HashSet<APIRouteGraphEdgeIntensity> outputIntensities =
				new HashSet<APIRouteGraphEdgeIntensity>(a_intensities);

			Dictionary<int, APIRouteGraphVertex> vertices = new Dictionary<int, APIRouteGraphVertex>(a_vertices.Length);
			foreach (APIRouteGraphVertex vertex in a_vertices)
			{
				vertices.Add(vertex.vertex_id, vertex);
			}

			Dictionary<int, List<APIRouteGraphEdgeIntensity>> intensitiesByEdgeId =
				new Dictionary<int, List<APIRouteGraphEdgeIntensity>>(a_intensities.Length);
			foreach (APIRouteGraphEdgeIntensity intensity in a_intensities)
			{
				if (!intensitiesByEdgeId.TryGetValue(intensity.edge_id,
					out List<APIRouteGraphEdgeIntensity> intensities))
				{
					intensities = new List<APIRouteGraphEdgeIntensity>(8);
					intensitiesByEdgeId.Add(intensity.edge_id, intensities);
				}

				intensities.Add(intensity);
			}

			Dictionary<APIRouteGraphVertex, List<APIRouteGraphEdge>> linksByVertex = new Dictionary<APIRouteGraphVertex, List<APIRouteGraphEdge>>(a_vertices.Length);
			foreach (APIRouteGraphEdge edge in a_edges)
			{
				List<APIRouteGraphEdge> vertexLinks;
				APIRouteGraphVertex fromVertex = vertices[edge.from_vertex_id];
				if (!linksByVertex.TryGetValue(fromVertex, out vertexLinks))
				{
					vertexLinks = new List<APIRouteGraphEdge>(8);
					linksByVertex.Add(fromVertex, vertexLinks);
				}

				if (!vertexLinks.Contains(edge))
				{
					vertexLinks.Add(edge);
				}

				APIRouteGraphVertex toVertex = vertices[edge.to_vertex_id];
				if (!linksByVertex.TryGetValue(toVertex, out vertexLinks))
				{
					vertexLinks = new List<APIRouteGraphEdge>(8);
					linksByVertex.Add(toVertex, vertexLinks);
				}
				if (!vertexLinks.Contains(edge))
				{
					vertexLinks.Add(edge);
				}
			}

			foreach (KeyValuePair<APIRouteGraphVertex, List<APIRouteGraphEdge>> kvp in linksByVertex)
			{
				if (kvp.Value.Count == 2)
				{
					if (Math.Abs(kvp.Value[0].edge_width - kvp.Value[1].edge_width) > 0.01f ||
					    !CrossesSameLayers(kvp.Value[0].link_crosses_msp_layers, kvp.Value[1].link_crosses_msp_layers))
					{
						continue;
					}

					GetVerticesForEdges(vertices, kvp.Value[0], kvp.Value[1], out APIRouteGraphVertex start, out APIRouteGraphVertex middle, out APIRouteGraphVertex end);
					Vector2D startEnd = new Vector2D(end.position_x, end.position_y) -
					                    new Vector2D(start.position_x, start.position_y);
					Vector2D startMiddle = new Vector2D(middle.position_x, middle.position_y) -
					                       new Vector2D(start.position_x, start.position_y);
					Vector2D middleEnd = new Vector2D(end.position_x, end.position_y) -
					                     new Vector2D(middle.position_x, middle.position_y);

					double startEndDistance = startEnd.Magnitude();
					double startMiddleEndDistance = startMiddle.Magnitude() + middleEnd.Magnitude();
					if (Math.Abs(startMiddleEndDistance - startEndDistance) < 0.01)
					{
						//Simplify
						int simplifiedEdgeId = kvp.Value[0].edge_id + kvp.Value[1].edge_id * 10000;

						List<APIRouteGraphEdgeIntensity> intensitiesFirst = intensitiesByEdgeId[kvp.Value[0].edge_id];
						List<APIRouteGraphEdgeIntensity> intensitiesSecond = intensitiesByEdgeId[kvp.Value[1].edge_id];
						bool intensitesCanBeSimplified = true;
						foreach (APIRouteGraphEdgeIntensity intensityFirst in intensitiesFirst)
						{
							APIRouteGraphEdgeIntensity intensitySecond =
								intensitiesSecond.Find(a_Obj => a_Obj.ship_type_id == intensityFirst.ship_type_id);

							if (intensitySecond == null)
							{
								intensitesCanBeSimplified = false;
								break;
							}
						}

						if (intensitesCanBeSimplified)
						{
							outputEdges.Remove(kvp.Value[0]);
							outputEdges.Remove(kvp.Value[1]);
							outputEdges.Add(new APIRouteGraphEdge(simplifiedEdgeId,
								start.vertex_id, end.vertex_id, kvp.Value[0].edge_width,
								kvp.Value[0].link_crosses_msp_layers));
							outputVertices.Remove(middle);

							foreach (APIRouteGraphEdgeIntensity intensityFirst in intensitiesFirst)
							{
								APIRouteGraphEdgeIntensity intensitySecond =
									intensitiesSecond.Find(a_Obj => a_Obj.ship_type_id == intensityFirst.ship_type_id);

								outputIntensities.Remove(intensityFirst);
								outputIntensities.Remove(intensitySecond);
								outputIntensities.Add(new APIRouteGraphEdgeIntensity(simplifiedEdgeId,
									intensityFirst.ship_type_id, intensityFirst.intensity + intensitySecond.intensity));

							}
						}
					}
				}
			}

			SimplifiedVertices = outputVertices.ToArray();
			SimplifiedEdges = outputEdges.ToArray();
			SimplifiedIntensities = outputIntensities.ToArray();

			Console.WriteLine(
				$"Simplified Vertices: {SimplifiedVertices.Length} (From {a_vertices.Length}) Edges: {SimplifiedEdges.Length} (From {a_edges.Length}) Intensities: {SimplifiedIntensities.Length} (From {a_intensities.Length})");
		}

		private bool CrossesSameLayers(APIGeometryType[] a_first, APIGeometryType[] a_second)
		{
			if (a_first == a_second)
			{
				return true;
			}

			if (a_first != null && a_second != null && a_first.Length == a_second.Length)
			{
				for (int i = 0; i < a_first.Length; ++i)
				{
					if (a_first[i] != a_second[i])
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		private void GetVerticesForEdges(Dictionary<int, APIRouteGraphVertex> a_vertexDictionary, APIRouteGraphEdge a_firstEdge, APIRouteGraphEdge a_secondEdge,
			out APIRouteGraphVertex a_start, out APIRouteGraphVertex a_middle, out APIRouteGraphVertex a_end)
		{
			if (a_firstEdge.from_vertex_id == a_secondEdge.to_vertex_id)
			{
				a_start = a_vertexDictionary[a_secondEdge.from_vertex_id];
				a_middle = a_vertexDictionary[a_firstEdge.from_vertex_id];
				a_end = a_vertexDictionary[a_firstEdge.to_vertex_id];
			}
			else if (a_firstEdge.to_vertex_id == a_secondEdge.from_vertex_id)
			{
				a_start = a_vertexDictionary[a_firstEdge.from_vertex_id];
				a_middle = a_vertexDictionary[a_firstEdge.to_vertex_id];
				a_end = a_vertexDictionary[a_secondEdge.to_vertex_id];
			}
			//This assumes directionality has already been resolved by the pathfinder.%
			else if (a_firstEdge.to_vertex_id == a_secondEdge.to_vertex_id)
			{
				a_start = a_vertexDictionary[a_firstEdge.from_vertex_id];
				a_middle = a_vertexDictionary[a_firstEdge.to_vertex_id];
				a_end = a_vertexDictionary[a_secondEdge.from_vertex_id];
			}
			else if (a_firstEdge.from_vertex_id == a_secondEdge.from_vertex_id)
			{
				a_start = a_vertexDictionary[a_firstEdge.to_vertex_id];
				a_middle = a_vertexDictionary[a_firstEdge.from_vertex_id];
				a_end = a_vertexDictionary[a_secondEdge.to_vertex_id];
			}
			else
			{
				throw new Exception();
			}
		}
	}
}
