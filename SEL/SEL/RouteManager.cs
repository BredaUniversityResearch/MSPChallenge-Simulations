using SEL.API;
using SEL.SpatialMapping;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing.Text;
using System.Globalization;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Poly2Tri.Triangulation.Delaunay;
using SEL.Issues;
using SEL.RasterizerLib;
using SEL.Routing;

namespace SEL
{
	/// <summary>
	/// Manager class that holds all the information required for routing. 
	/// Contains references to all the vertices, edges and available routes from port to port.
	/// Also is responsible for (re)generating implicit edges and the routes.
	/// </summary>
	class RouteManager
	{
		private const string SHIPPING_LANE_WIDTH_META_KEY = "ShippingWidth";
		private const string SHIPPING_DIRECTION_META_KEY = "ShippingDirection";
		private const string DIRECTION_FORWARD = "Forward";
		private const string DIRECTION_REVERSE = "Reverse";
		private const string DIRECTION_BIDIRECTIONAL = "Bidirectional";
		private static readonly Vector2D RESTRICTION_MESH_AREA_LEEWAY = new Vector2D(100000, 100000); //Additional size for the simulation area to accomodate restriction meshes.

		private const int NUM_DIRECTIONAL_SLICES = 8; //Number of directional slices that we want to try and find a connection for.

		private ShippingIssueManager m_issueManager;
		private List<LaneVertex> m_lanePoints = new List<LaneVertex>();
		private QuadTree<LaneVertex> m_lanePointSpatialMap = null;
		private List<LaneEdge> m_laneEdges = new List<LaneEdge>();
		private QuadTree<RestrictionEdge> m_restrictionEdges = null;
		private QuadTree<RestrictionMesh> m_restrictionMeshes = null;

		private List<LaneEdgeString> m_laneEdgeStrings = new List<LaneEdgeString>();
		private ConcurrentDictionary<ulong, Route> m_availableRoutes = new ConcurrentDictionary<ulong, Route>();

		public RouteManager(ShippingIssueManager issueManager)
		{
			m_issueManager = issueManager;
		}

		public IEnumerable<LaneVertex> GetVertices()
		{
			return m_lanePoints;
		}

		public IEnumerable<LaneEdge> GetEdges()
		{
			return m_laneEdges;
		}

		public LaneEdge GetEdgeByIndex(int index)
		{
			return m_laneEdges[index];
		}

		public IEnumerable<RestrictionEdge> GetRestrictionEdges()
		{
			return m_restrictionEdges.GetValueIterator();
		}

		public IEnumerable<RestrictionMesh> GetRestrictionMeshes()
		{
			return m_restrictionMeshes.GetValueIterator();
		}

		public void RasterizeRestrictionMeshes(Rect rasterizerBounds, Vector2 rasterOutputResolution)
		{
			foreach (RestrictionMesh mesh in m_restrictionMeshes.GetValueIterator())
			{
				mesh.m_rasterSpaceTriangulatedMesh.Clear();
				Rasterizer.RasterizeMeshRescaleToBounds(mesh, rasterizerBounds, rasterOutputResolution, mesh.m_rasterSpaceTriangulatedMesh);
				List<DelaunayTriangle> worldSpaceTriangles = new List<DelaunayTriangle>(mesh.m_rasterSpaceTriangulatedMesh.Count);
				Rasterizer.RasterizeMesh(mesh, worldSpaceTriangles);

				try
				{
					mesh.SetWorldSpaceTriangulatedMesh(worldSpaceTriangles);
				}
				catch (GeometryOutOfBoundsException ex)
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						$"Failure importing rasterized restriction meshes into spatial data tree. Geometry ID: {mesh.m_geometryId} on Layer ID: {mesh.m_layerId} has parts of geometry that fall outside of the playable bounds");
					ErrorReporter.ReportError(EErrorSeverity.Error, $"Exception message: {ex.Message}");
				}
			}
		}

		/// <summary>
		/// Sets the simulation area to the defined geometry so we know how large to set our spatial structures.
		/// </summary>
		/// <param name="simulationArea">The world space bounds of the simulation area</param>
		public void SetSimulationArea(AABB simulationArea)
		{
			m_lanePointSpatialMap = new QuadTree<LaneVertex>(simulationArea);
			m_restrictionEdges = new QuadTree<RestrictionEdge>(simulationArea);
			m_restrictionMeshes = new QuadTree<RestrictionMesh>(new AABB(simulationArea.min - RESTRICTION_MESH_AREA_LEEWAY, simulationArea.max + RESTRICTION_MESH_AREA_LEEWAY));
		}

		/// <summary>
		/// Imports all lanes defined by the API geometry
		/// </summary>
		/// <param name="shippingLaneGeometry"></param>
		/// <param name="shipTypeManager"></param>
		/// <param name="regionSettings"></param>
		public void ImportLanes(APIShippingLaneGeometry[] shippingLaneGeometry, ShipTypeManager shipTypeManager, APISELRegionSettings regionSettings)
		{
			foreach (APIShippingLaneGeometry geometry in shippingLaneGeometry)
			{
				ShipType[] allowedShipTypes = null;
				if (geometry.ship_type_ids != null)
				{
					allowedShipTypes = new ShipType[geometry.ship_type_ids.Length];
					for (int i = 0; i < geometry.ship_type_ids.Length; ++i)
					{
						allowedShipTypes[i] = shipTypeManager.FindShipTypeById(geometry.ship_type_ids[i]);
						if (allowedShipTypes[i] == null)
						{
							ErrorReporter.ReportError(EErrorSeverity.Error, "Could not find ship type with ID " + geometry.ship_type_ids[i] + " for shipping lane geometry. Please verify the \"layer_type_restriction_group_mapping\"");
						}
					}
				}

				FindDirectionalityProperty(geometry, out EEdgeDirectionality directionality, out bool reverseOrder);
				float laneWidth = -1.0f;
				if (geometry.geometry_data != null && geometry.geometry_data.TryGetValue(SHIPPING_LANE_WIDTH_META_KEY, out string laneWidthMetaString))
				{
					laneWidth = float.Parse(laneWidthMetaString, CultureInfo.InvariantCulture);
				}

				LaneEdgeString edgeString = new LaneEdgeString(geometry.geometry_id);
				LaneVertex lastVertex = null;
				try
				{
					for (int i = 0; i < geometry.geometry.Length; ++i)
					{
						int vertexIndex = (reverseOrder) ? geometry.geometry.Length - i - 1 : i;

						double[] point = geometry.geometry[vertexIndex];
						Vector2D newPoint = new Vector2D(point[0], point[1]);
						if (lastVertex != null)
						{
							double distance = (lastVertex.position - newPoint).Magnitude();
							if (distance > regionSettings.shipping_lane_point_merge_distance ||
							    i == geometry.geometry.Length - 1)
							{
								LaneVertex newVertex = CreateLaneVertex(newPoint.x, newPoint.y, geometry.geometry_id);
								int subdivisionPoints =
									(int) Math.Floor(distance / regionSettings.shipping_lane_subdivide_distance);
								CreateSubdividedEdge(lastVertex, newVertex, edgeString, allowedShipTypes,
									ELaneEdgeType.Persistent, subdivisionPoints, directionality, laneWidth);
								lastVertex = newVertex;
							}
							else
							{
								//Merge the two vertices by just not updating the 'last vertex' we have now. 
							}
						}
						else
						{
							lastVertex = CreateLaneVertex(newPoint.x, newPoint.y, geometry.geometry_id);
						}
					}
				}
				catch (GeometryOutOfBoundsException ex)
				{
					ErrorReporter.ReportError(EErrorSeverity.Error,
						$"Failure during importing of shipping lanes. Geometry ID: {geometry.geometry_id} has geometry parts outside of the playable area");
					ErrorReporter.ReportError(EErrorSeverity.Error, $"Exception message: {ex.Message}");
				}

				m_laneEdgeStrings.Add(edgeString);
			}
		}

		private static void FindDirectionalityProperty(APIShippingLaneGeometry geometry, out EEdgeDirectionality directionality, out bool reverseOrder)
		{
			if (geometry.geometry_data != null && geometry.geometry_data.TryGetValue(SHIPPING_DIRECTION_META_KEY, out var directionalityProperty))
			{
				if (directionalityProperty == DIRECTION_BIDIRECTIONAL)
				{
					directionality = EEdgeDirectionality.Bidirectional;
					reverseOrder = false;
				}
				else if (directionalityProperty == DIRECTION_FORWARD)
				{
					directionality = EEdgeDirectionality.Unidirectional;
					reverseOrder = false;
				}
				else if (directionalityProperty == DIRECTION_REVERSE)
				{
					directionality = EEdgeDirectionality.Unidirectional;
					reverseOrder = true;
				}
				else
				{
					throw new Exception(
						$"Unknown directionality meta property value ({SHIPPING_DIRECTION_META_KEY}:{directionalityProperty})");
				}
			}
			else
			{
				directionality = EEdgeDirectionality.Bidirectional;
				reverseOrder = false;
			}
		}

		private LaneVertex CreateLaneVertex(double x, double y, int geometryId)
		{
			LaneVertex vertex = new LaneVertex(x, y, geometryId);
			m_lanePoints.Add(vertex);
			AABB lanePointBounds = new AABB(new Vector2D(x, y), new Vector2D(x, y));
			if (m_lanePointSpatialMap.GetRootBounds().IntersectsWith(lanePointBounds))
			{
				m_lanePointSpatialMap.Insert(lanePointBounds, vertex);
			}
			return vertex;
		}

		/// <summary>
		/// Thrashes and rebuilds all the implicit (generated) edges in the graph.
		/// </summary>
		public void RebuildImplicitEdges(double maxImplicitEdgeDistance, ShippingPortManager portManager, double maxPortMergeDistance)
		{
			//Throw away all implicitly generated lanes.
			m_laneEdges.RemoveAll(obj => obj.m_laneType == ELaneEdgeType.Implicit || obj.m_laneType == ELaneEdgeType.Merge);

			foreach (ShippingPort port in portManager.GetAllPortsByType(EShippingPortType.DefinedPort))
			{
				MergeToNearbyVertices(port.PortPathingVertex, maxPortMergeDistance);
			}

			for (int fromVertexId = 0; fromVertexId < m_lanePoints.Count; ++fromVertexId)
			{
				LaneVertex fromVertex = m_lanePoints[fromVertexId];
				//Connect all vertices that are at almost the same locations. 
				MergeToNearbyVertices(fromVertex, 50.0);

				//Now find the closest vertex in a couple of directions and connect with those.
				double directionalSliceSizeInRadians = Math.PI * 2.0 / (double)(NUM_DIRECTIONAL_SLICES);
				double directionalSliceAngleLimit = Math.Cos(directionalSliceSizeInRadians * 0.5);
				for (int i = 0; i < NUM_DIRECTIONAL_SLICES; ++i)
				{
					double directionX = Math.Cos((double)i * directionalSliceSizeInRadians);
					double directionY = Math.Sin((double)i * directionalSliceSizeInRadians);

					LaneVertex foundVertex = FindClosestVertexInDirection(fromVertex, new Vector2D(directionX, directionY), directionalSliceAngleLimit, maxImplicitEdgeDistance);
					
					if (foundVertex != null)
					{
						if (FindEdge(fromVertex, foundVertex) == null)
						{
							//Now to figure out what groups can cross our newly created lane. This is a mask keyed by the restriction groups of ships.
							RestrictionGeometryType restrictionType = GetRestrictionCapabilityMask(fromVertex, foundVertex, out float restrictionOverlapAmount);
							if (restrictionType.GetAllowedShipTypeMask() != 0)
							{
								LaneEdge newEdge = CreateEdge(fromVertex, foundVertex, ELaneEdgeType.Implicit, EEdgeDirectionality.Bidirectional, LaneEdge.LaneWidthNotDefined);
								newEdge.SetRestrictionType(restrictionType, restrictionOverlapAmount);
							}
						}
					}
				}
			}
		}

		private void MergeToNearbyVertices(LaneVertex fromVertex, double maxMergeDistance)
		{
			List<LaneVertex> nearVertices = new List<LaneVertex>();
			FindNearbyVertices(fromVertex, maxMergeDistance, ref nearVertices);
			foreach (LaneVertex nearbyVertex in nearVertices)
			{
				if (FindEdge(fromVertex, nearbyVertex) == null)
				{
					CreateEdge(fromVertex, nearbyVertex, ELaneEdgeType.Merge, EEdgeDirectionality.Bidirectional, LaneEdge.LaneWidthNotDefined);
				}
			}
		}

		private LaneVertex FindClosestUnconnectedReachableVertexInDirection(LaneVertex from, Vector2D direction, double angleLimit)
		{
			LaneVertex result = null;
			List<LaneVertex> vertices = new List<LaneVertex>(32);
			FindVerticesInDirection(from, direction, angleLimit, 32, 1e25f, ref vertices);

			foreach (LaneVertex vertex in vertices)
			{
				if (FindEdge(from, vertex) == null)
				{
					RestrictionGeometryType restrictionType = GetRestrictionCapabilityMask(from, vertex, out float restrictionOverlapAmount);
					if (restrictionType.GetAllowedShipTypeMask() != 0)
					{
						result = vertex;
						break;
					}
				}
			}
			return result;
		}

		private RestrictionGeometryType GetRestrictionCapabilityMask(LaneVertex from, LaneVertex to, out float restrictionOverlapAmount)
		{
			restrictionOverlapAmount = 0.0f;
			GeometryEdge temporaryEdge = new GeometryEdge(from, to);
			SpatialQueryAABB query = new SpatialQueryAABB(temporaryEdge.CalculateBounds());
			QuadTreeSpatialQueryResult<RestrictionEdge> results = m_restrictionEdges.Query(query);
			QuadTreeSpatialQueryResult<RestrictionMesh> meshResults = m_restrictionMeshes.Query(query);

			HashSet<RestrictionGeometryType> compoundTypeList = new HashSet<RestrictionGeometryType>(RestrictionGeometryType.EqualityComparer.Instance);
			Dictionary<RestrictionMesh, float> intersectionSegmentSizes = new Dictionary<RestrictionMesh, float>(); 

			int remainingShipRestrictionMask = 0x7fffffff;
			foreach (RestrictionEdge restrictionEdge in results)
			{
				if (restrictionEdge.IntersectionTest(temporaryEdge, out float intersectionTime))
				{
					remainingShipRestrictionMask &= restrictionEdge.GetRestrictionType().GetAllowedShipTypeMask();
					if (remainingShipRestrictionMask == 0)
					{
						return RestrictionGeometryType.DisallowAll;
					}
					else
					{
						float segmentPosition = intersectionTime;
						if (intersectionSegmentSizes.TryGetValue(restrictionEdge.m_parentMesh, out float existingSegmentPosition))
						{
							float segmentOverlap;
							if (existingSegmentPosition > segmentPosition)
							{
								segmentOverlap = existingSegmentPosition - segmentPosition;
							}
							else
							{
								segmentOverlap = segmentPosition - existingSegmentPosition;
							}

							if (segmentOverlap < 0.0f)
							{
								throw new Exception("Segment overlap is smaller than 0");
							}

							intersectionSegmentSizes[restrictionEdge.m_parentMesh] = segmentOverlap;
						}
						else
						{
							if (restrictionEdge.m_parentMesh.ContainsPoint(to.position))
							{
								segmentPosition = 1.0f - segmentPosition;
							}

							intersectionSegmentSizes.Add(restrictionEdge.m_parentMesh, segmentPosition);
							compoundTypeList.Add(restrictionEdge.GetRestrictionType());
						}
					}
				}
			}

			foreach(float segmentSize in intersectionSegmentSizes.Values)
			{
				restrictionOverlapAmount += segmentSize;
			}

			restrictionOverlapAmount = Math.Min(1.0f, restrictionOverlapAmount);

			//Check meshes so we don't miss any meshes that completely encase us.
			foreach (RestrictionMesh mesh in meshResults)
			{
				if (!intersectionSegmentSizes.ContainsKey(mesh))
				{
					bool fromEncased = mesh.ContainsPoint(from.position);
					bool toEncased = mesh.ContainsPoint(to.position);
					if (fromEncased && toEncased)
					{
						remainingShipRestrictionMask &= mesh.m_restrictionType.GetAllowedShipTypeMask();
						if (remainingShipRestrictionMask == 0)
						{
							return RestrictionGeometryType.DisallowAll;
						}
						else
						{
							compoundTypeList.Add(mesh.m_restrictionType);
							restrictionOverlapAmount = 1.0f;
						}
					}
					else if (fromEncased != toEncased)
					{
						ErrorReporter.ReportError(EErrorSeverity.Error, $"Sanity Check: We probably should have had an intersection before?\nEdge From: {from.vertexId} to: {to.vertexId}");
					}
				}
			}

			if (compoundTypeList.Count == 0)
			{
				return RestrictionGeometryType.AllowAll;
			}
			else
			{
				return RestrictionGeometryType.CreateCompoundType(compoundTypeList);
			}
		}

		private LaneEdge CreateEdge(LaneVertex fromPoint, LaneVertex toPoint, ELaneEdgeType edgeType, EEdgeDirectionality edgeDirectionality, float edgeWidth)
		{
			LaneEdge edge = new LaneEdge(fromPoint, toPoint, edgeType, edgeDirectionality, edgeWidth);
			m_laneEdges.Add(edge);
			fromPoint.AddConnection(edge);
			toPoint.AddConnection(edge);
			return edge;
		}

		private LaneEdge CreateEdge(LaneVertex fromVertex, LaneVertex toVertex, ELaneEdgeType laneEdgeType, EEdgeDirectionality edgeDirectionality, float edgeWidth, ShipType[] allowedShipTypes)
		{
			LaneEdge edge = CreateEdge(fromVertex, toVertex, laneEdgeType, edgeDirectionality, edgeWidth);
			if (allowedShipTypes != null)
			{
				edge.SetAllowedShipTypes(allowedShipTypes);
			}
			return edge;
		}

		private void CreateSubdividedEdge(LaneVertex fromPoint, LaneVertex toPoint, LaneEdgeString edgeString, ShipType[] allowedShipTypes, ELaneEdgeType laneEdgeType, int numSubdivisionPoints, EEdgeDirectionality directionality, float laneWidth)
		{
			LaneVertex lastVertex = fromPoint;
			float timeSlicePerSubdivisionPoint = 1.0f / (float)(numSubdivisionPoints + 1);
			for (int i = 0; i < numSubdivisionPoints; ++i)
			{
				float timeSlice = timeSlicePerSubdivisionPoint * (float)(i + 1);
				Vector2D newVertexPosition = Vector2D.Lerp(fromPoint.position, toPoint.position, timeSlice);

				LaneVertex newVertex = CreateLaneVertex(newVertexPosition.x, newVertexPosition.y, fromPoint.geometryId);
				LaneEdge createdEdge = CreateEdge(lastVertex, newVertex, laneEdgeType, directionality, laneWidth, allowedShipTypes);
				edgeString.AddNextEdge(createdEdge);
				lastVertex = newVertex;
			}
			LaneEdge lastEdge = CreateEdge(lastVertex, toPoint, laneEdgeType, directionality, laneWidth, allowedShipTypes);
			edgeString.AddNextEdge(lastEdge);
		}

		private LaneEdge FindEdge(LaneVertex from, LaneVertex to)
		{
			LaneEdge result = null;
			foreach(LaneEdge edge in from.GetConnections())
			{
				if ((edge.m_from == from || edge.m_from == to) &&
					(edge.m_to == from || edge.m_to == to))
				{
					result = edge;
					break;
				}
			}
			return result;
		}

		private LaneEdgeString FindLaneEdgeStringByGeometryId(int laneEdgeGeometryId)
		{
			LaneEdgeString result = null;
			foreach(LaneEdgeString edgeString in m_laneEdgeStrings)
			{
				if (edgeString.laneGeometryId == laneEdgeGeometryId)
				{
					result = edgeString;
					break;
				}
			}
			return result;
		}

		private void FindVerticesInDirection(LaneVertex fromVertex, Vector2D direction, double dotProductLimit, int vertexLimit, double maxDistance, ref List<LaneVertex> result)
		{
			int foundCount = 0;
			foreach (LaneVertex vertex in m_lanePoints)
			{
				if (vertex == fromVertex)
				{
					continue;
				}

				Vector2D deltaPos = vertex.position - fromVertex.position;
				double distance = deltaPos.Magnitude();
				if (distance < maxDistance)
				{
					deltaPos *= (1.0f / distance);
					double dotResult = direction.DotProduct(deltaPos);
					if (dotResult >= dotProductLimit)
					{
						result.Add(vertex);
						++foundCount;
						if (foundCount > vertexLimit)
						{
							break;
						}
					}
				}
			}
		}

		private LaneVertex FindVertexByGeometryId(int geometryId)
		{
			LaneVertex result = null;
			foreach (LaneVertex vertex in m_lanePoints)
			{
				if (vertex.geometryId == geometryId)
				{
					result = vertex;
					break;
				}
			}
			return result;
		}

		private LaneVertex FindClosestVertexInDirection(LaneVertex fromVertex, Vector2D direction, double dotProductLimit, double distanceLimit)
		{
			List<LaneVertex> foundVertices = new List<LaneVertex>();
			FindVerticesInDirection(fromVertex, direction, dotProductLimit, 256, distanceLimit, ref foundVertices);

			LaneVertex closestVertex = null;
			double closestDistance = 1e25;
			foreach (LaneVertex vertex in foundVertices)
			{
				double distance = (fromVertex.position - vertex.position).MagnitudeSqr();
				if (distance < closestDistance)
				{
					closestDistance = distance;
					closestVertex = vertex;
				}
			}

			return closestVertex;
		}

		private void FindNearbyVertices(LaneVertex fromVertex, double maxDistance, ref List<LaneVertex> result)
		{
			AABB bounds = new AABB(	new Vector2D(fromVertex.position.x - maxDistance, fromVertex.position.y - maxDistance),
									new Vector2D(fromVertex.position.x + maxDistance, fromVertex.position.y + maxDistance));
			QuadTreeSpatialQueryResult<LaneVertex> spatialQueryResult = m_lanePointSpatialMap.Query(new SpatialQueryAABB(bounds));

			double sqrMaxDistance = maxDistance * maxDistance;
			foreach (LaneVertex vertex in spatialQueryResult)
			{
				if (vertex != fromVertex)
				{
					Vector2D deltaPosition = vertex.position - fromVertex.position;
					double distance = deltaPosition.MagnitudeSqr();
					if (distance < sqrMaxDistance)
					{
						result.Add(vertex);
					}
				}
			}
		}

		public LaneVertex CreatePortVertex(Vector2D position)
		{
			return CreateLaneVertex(position.x, position.y, -1);
		}

		public void ImportRestrictions(APIShippingRestrictionGeometry[] restrictionGeometry, RestrictionGeometryTypeManager restrictionTypeManager, double restrictionPointSize)
		{
			foreach (APIShippingRestrictionGeometry restriction in restrictionGeometry)
			{
				RestrictionGeometryType restrictionGeomType = restrictionTypeManager.GetAllowedShipMask(restriction.layer_id, restriction.layer_types);

				if (restriction.geometry.Length == 1)
				{
					restriction.geometry = GeometryUtilities.CreateSquareGeometryDataCenteredAt(new Vector2D(restriction.geometry[0][0], restriction.geometry[0][1]), 
						new Vector2D(restrictionPointSize * 0.5, restrictionPointSize * 0.5));
				}

				RestrictionMesh restrictionMesh = new RestrictionMesh(restriction.geometry_id, restriction.layer_id, restrictionGeomType, restriction.geometry);
				try
				{
					m_restrictionMeshes.Insert(restrictionMesh.m_bounds, restrictionMesh);
				}
				catch (GeometryOutOfBoundsException ex)
				{
					ErrorReporter.ReportError(EErrorSeverity.Error, $"Failure during importing restriction geometry. Geometry ID: {restrictionMesh.m_geometryId}, Layer ID: {restrictionMesh.m_layerId}");
					ErrorReporter.ReportError(EErrorSeverity.Error, "Exception text " + ex.Message);
					//throw;
				}

				GeometryVertex previousVertex = null;
				foreach(double[] geometryLine in restriction.geometry)
				{
					GeometryVertex currentVertex = new GeometryVertex(geometryLine[0], geometryLine[1]);
					if (previousVertex != null)
					{
						RestrictionEdge instance = new RestrictionEdge(previousVertex, currentVertex, restrictionGeomType, restrictionMesh);
						AABB instanceBounds = instance.CalculateBounds();
						if (m_restrictionEdges.GetRootBounds().IntersectTest(instanceBounds) != EIntersectResult.NoIntersection)
						{
							try
							{
								m_restrictionEdges.Insert(instanceBounds, instance);
							}
							catch (GeometryOutOfBoundsException ex)
							{
								ErrorReporter.ReportError(EErrorSeverity.Error,
									$"Failure during import of restriction geometry. Geometry ID: {restriction.geometry_id}, on layer ID : {restriction.layer_id} has parts outside of the playable bounds");
								ErrorReporter.ReportError(EErrorSeverity.Error, "Exception text " + ex.Message);
							}
						}
					}

					previousVertex = currentVertex;
				}
			}
		}

		public void UpdateRoutes(RouteIntensityManager routeIntensityManager, ShipTypeManager shipTypeManager)
		{
			IReadOnlyList<RoutingEntry> entriesToRoute = routeIntensityManager.GetCurrentRoutes();
			ConcurrentQueue<RoutingEntry> queuedRoutingJobs = new ConcurrentQueue<RoutingEntry>(entriesToRoute);

			int maxParallelRouteFindingTasks = SELConfig.Instance.RouteFinderParallelTasks();
			Task[] runningTasks = new Task[maxParallelRouteFindingTasks];
			for (int i = 0; i < maxParallelRouteFindingTasks; ++i)
			{
				runningTasks[i] = Task.Run(() => { UpdateRouteThreaded(queuedRoutingJobs, shipTypeManager); });
			}

			Task.WaitAll(runningTasks);
		}

		private void UpdateRouteThreaded(ConcurrentQueue<RoutingEntry> jobsToProcess, ShipTypeManager shipTypeManager)
		{
			while (!jobsToProcess.IsEmpty)
			{
				if (jobsToProcess.TryDequeue(out var routingJob))
				{
					LaneVertex startVertex = routingJob.sourcePort.PortPathingVertex;
					LaneVertex destinationVertex = routingJob.destinationPort.PortPathingVertex;

					if (startVertex != null && destinationVertex != null)
					{
						Route cachedRoute = FindCachedRoute(routingJob.shipTypeId, startVertex.vertexId, destinationVertex.vertexId);
						if (cachedRoute == null)
						{
							ShipType shipType = shipTypeManager.FindShipTypeById(routingJob.shipTypeId);

							Route foundRoute = RouteFinder.FindRoute(startVertex, destinationVertex, shipType);
							if (foundRoute == null)
							{
								OnRouteFindingFailed(routingJob.sourcePort, routingJob.destinationPort, shipType);
							}

							//Even when route finding fails (route == null) we still cache this 'failed' result.
							if (!m_availableRoutes.TryAdd(
								CreateRouteHash(shipType.ShipTypeId, startVertex.vertexId, destinationVertex.vertexId), foundRoute))
							{
								if (foundRoute != null)
								{
									Console.WriteLine(
										"Failed to add valid available route to the available routes collection. Probably due to concurrency?");
								}
							}
						}
					}
				}
			}
		}

		private void OnRouteFindingFailed(ShippingPort source, ShippingPort destination, ShipType shipType)
		{
			lock (m_issueManager)
			{
				m_issueManager.AddRoutingFailureIssue(source, destination, shipType);
			}

			Console.WriteLine(
				$"Could not find route from port \"{source.PortName}\" to port \"{destination.PortName}\" for ship type \"{shipType.ShipTypeId}\"");
		}

		public IEnumerable<Route> GetAvailableRoutes()
		{
			return m_availableRoutes.Values;
		}

		public int GetAvailableRouteCount()
		{
			return m_availableRoutes.Count;
		}

		public Route FindCachedRoute(RouteIntensity requestedRoute)
		{
			return FindCachedRoute(requestedRoute.ShipTypeId, requestedRoute.SourcePort.PortPathingVertex.vertexId,
				requestedRoute.DestinationPort.PortPathingVertex.vertexId);
		}

		public Route FindCachedRoute(byte shipTypeId, int fromVertexId, int toVertexId)
		{
			Route result = null;
			//Check for a route that goes from our port to the destination
			if (!m_availableRoutes.TryGetValue(CreateRouteHash(shipTypeId, fromVertexId, toVertexId), out result))
			{
				//Check for a route that goes from the destination port to our port, but make sure this route is bidirectional.
				if (m_availableRoutes.TryGetValue(CreateRouteHash(shipTypeId, toVertexId, fromVertexId), out result))
				{
					if (result != null && result.Directionality == ERouteDirectionality.Unidirectional)
					{
						//Invalid route, cannot traverse a unidirectional route from destination to source.
						result = null;
					}
				}
			}

			return result;
		}

		public IEnumerable<Route> FindRoutesForPort(ShippingPort port, byte shipTypeId)
		{
			LaneVertex vertex = port.PortPathingVertex;
			List<Route> result = new List<Route>();
			if (vertex != null)
			{
				foreach (KeyValuePair<ulong, Route> routeKvp in m_availableRoutes)
				{
					if (routeKvp.Value != null)
					{
						if (routeKvp.Value.ShipTypeInfo.ShipTypeId == shipTypeId &&
							(routeKvp.Value.FromVertex.vertexId == vertex.vertexId || routeKvp.Value.ToVertex.vertexId == vertex.vertexId))
						{
							result.Add(routeKvp.Value);
						}
					}
				}
			}
			return result;
		}

		private static ulong CreateRouteHash(byte shipTypeId, int fromVertexId, int toVertexId)
		{
			//ShipType	: 8
			//FromVertex: 28
			//ToVertex	: 28
			//Total:	: 64 bits

			const int VERTEX_ID_MASK = 0x0fffffff;

			if (shipTypeId > 255)
				throw new ArgumentOutOfRangeException($"ShipTypeId out of range, should be lower than 255, got {shipTypeId}");
			if ((fromVertexId & VERTEX_ID_MASK) != fromVertexId)
				throw new ArgumentOutOfRangeException($"FromVertexId out of range, should be lower than {VERTEX_ID_MASK}, got {fromVertexId}");
			if ((toVertexId & VERTEX_ID_MASK) != toVertexId)
				throw new ArgumentOutOfRangeException($"ToVertexId out of range, should be lower than {VERTEX_ID_MASK}, got {toVertexId}");
			return ((ulong)(long)(shipTypeId & 0xff) | ((ulong)(long)(fromVertexId & 0x0fffffff) << 8) | ((ulong)(toVertexId & 0x0fffffff) << 36));
		}

		public static ulong CreateRouteHash(Route route)
		{
			return CreateRouteHash(route.ShipTypeInfo.ShipTypeId, route.FromVertex.vertexId, route.ToVertex.vertexId);
		}
	}
}
