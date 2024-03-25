using System;
using System.Collections.Generic;

// Original C++ implementations of Dinic's Algorithm found at: http://126kr.com/article/469et77xphm and https://sites.google.com/site/indy256/algo_cpp/dinic_flow
class Dinic
{
    public const long LONG_MAX = long.MaxValue;
    int numberNodes;
    int inOutOffset;
    int[] distance;
    bool runCorrectly;

	/* All nodes are split into in and out, with the edge between representing the node's capacity.
     * out nodes are at normal indices, in nodes are at indices + inOutOffset. 			
	 */
	Dictionary<int, int> nodeIDtoIndex;
    Dictionary<int, int> nodeIndextoID;
	Dictionary<int, Edge> cableIDtoEdge;
    List<List<Edge>> adj;
	
	int sourceIndex, sinkIndex;	//Paths energy is sent along
	int[] timesEdgesVisited;	//The next edge index to visit when a node is explored
	List<FlowPath> flowPaths;	//To log results

    int GetIndexForNodeID(int nodeID)
    {
        int result = 0;
        if (!nodeIDtoIndex.TryGetValue(nodeID, out result))
        {
            Console.WriteLine("ERROR\t| Connection to node ID that does not exist. Node ID: " + nodeID);
            runCorrectly = false;
        }
        return result;
    }

    int GetNodeIDForIndex(int index)
    {
        int result = 0;
        if (!nodeIndextoID.TryGetValue(index, out result))
        {
            Console.WriteLine("ERROR\t| Getting node ID for index that does not exist. Index: " + index);
            runCorrectly = false;
        }
        return result;
    }

    public Dinic(List<InputNode> nodes, List<Connection> initialConnections)
    {
        nodeIDtoIndex = new Dictionary<int, int>();
		nodeIndextoID = new Dictionary<int, int>();
        cableIDtoEdge = new Dictionary<int, Edge>();
        inOutOffset = nodes.Count;
        numberNodes = inOutOffset * 2;
        adj = new List<List<Edge>>();

        //Create list of Edges per Node. Keep track of indices.
        for (int i = 0; i < numberNodes; i++)
        {
            adj.Add(new List<Edge>());
			if (i < inOutOffset)
			{
				nodeIDtoIndex.Add(nodes[i].geometry_id, i);
				nodeIndextoID.Add(i, nodes[i].geometry_id);
			}
        }

        //Create the edge between the 2 parts of the node
        for (int i = 0; i < numberNodes / 2; i++) 
            AddSingleEdge(i + inOutOffset, i, nodes[i].maxcapacity, nodes[i].geometry_id);
        
        //Add the initial connections
        foreach (Connection con in initialConnections)
            AddBidirectionalEdge(con.fromNodeID, con.toNodeID, con.maxcapacity, con.cableID, true);

        distance = new int[numberNodes];
    }

    public void AddBidirectionalEdgeWithCapOfNode(int fromNodeID, int toNodeID, int capID, int geomID, bool initialEdge = false)
    {
		//adj[nodeID][0] is always the edge to the other part of the node, so that edge's capacity is the node capacity
		//We need the edge from in to out, so add inoutoffset
        AddBidirectionalEdge(fromNodeID, toNodeID, adj[GetIndexForNodeID(capID) + inOutOffset][0].cap, geomID, initialEdge);
    }

    public void AddBidirectionalEdge(int fromNodeID, int toNodeID, long cap, int geomID, bool initialEdge = false)
    {
        int fromNodeIndex = GetIndexForNodeID(fromNodeID);
        int toNodeIndex = GetIndexForNodeID(toNodeID);
        if (!initialEdge) //initial edges don't have to be added to the dict because they don't correspond to actual cables
            cableIDtoEdge.Add(geomID, AddSingleEdge(fromNodeIndex, toNodeIndex + inOutOffset, cap, geomID));
        else
            AddSingleEdge(fromNodeIndex, toNodeIndex + inOutOffset, cap, geomID);
        AddSingleEdge(toNodeIndex, fromNodeIndex + inOutOffset, cap, geomID);
    }

    private Edge AddSingleEdge(int fromNodeIndex, int toNodeIndex, long cap, int geomID)
    {
        if (fromNodeIndex < 0 || fromNodeIndex >= adj.Count || toNodeIndex < 0 || toNodeIndex >= adj.Count)
        {
            runCorrectly = false;
            return null;
        }

        Edge edge = new Edge(toNodeIndex, 0, cap, adj[toNodeIndex].Count, geomID);
        adj[fromNodeIndex].Add(edge);
        adj[toNodeIndex].Add(new Edge(fromNodeIndex, 0, 0, adj[fromNodeIndex].Count - 1, geomID));
        return edge;
    }

    public bool FillDistancesBFS()
    {
        for (int i = 0; i < numberNodes; i++)
            distance[i] = -1;

        distance[sourceIndex] = 0;
        Queue<int> q = new Queue<int>();
        q.Enqueue(sourceIndex);

        while (q.Count > 0)
        {
            int current = q.Dequeue();
            foreach (Edge edge in adj[current])
                if (distance[edge.index] < 0 && edge.HasCapacityLeft)
                {
                    // Level of current vertex is, level of parent + 1
                    distance[edge.index] = distance[current] + 1;
                    q.Enqueue(edge.index);
                }
        }
        //Return whether the sink can be reached
        return distance[sinkIndex] >= 0;
    }

    public long SendFlow(int fromNodeIndex, long maxFlow)
    {
        // Sink reached
        if (fromNodeIndex == sinkIndex)
            return maxFlow;

        //DFS to toNode
        while (timesEdgesVisited[fromNodeIndex] < adj[fromNodeIndex].Count)
        {
            Edge edge = adj[fromNodeIndex][timesEdgesVisited[fromNodeIndex]];
            if (distance[edge.index] == distance[fromNodeIndex] + 1 && edge.HasCapacityLeft)
            {
				// find minimum flow from the node to the sink
				long temp_flow = SendFlow(edge.index, Math.Min(maxFlow, edge.cap - edge.flow));

                if (temp_flow > 0)
                {
                    //Add flow to current edge
                    edge.flow += temp_flow;

                    //Subtract flow from reverse edge from current edge
                    adj[edge.index][edge.rev].flow -= temp_flow;
					if (fromNodeIndex < inOutOffset)
					{
						int id = GetNodeIDForIndex(fromNodeIndex);
						if(id > 0)
							flowPaths[flowPaths.Count - 1].path.Add(id);
					}
					return temp_flow;
                }
            }
			timesEdgesVisited[fromNodeIndex]++;
        }
        return 0;
    }
    public bool DinicMaxflow(int sourceID, int sinkID, out long result)
    {
        runCorrectly = true;
		flowPaths = new List<FlowPath>();

        sourceIndex = nodeIDtoIndex[sourceID] + inOutOffset;
        sinkIndex = nodeIDtoIndex[sinkID];

        if (sourceIndex == sinkIndex)
        {
            result = -1;
            return false;
        }

		long maxFlow = 0;
        // Augment the flow while there is path from source to sink
        while (FillDistancesBFS())
        {
            timesEdgesVisited = new int[numberNodes + 1];

			//Augment flow if a path can be found
			long flow = 1;
            while (flow != 0)
            {
				flowPaths.Add(new FlowPath());
                flow = SendFlow(sourceIndex, LONG_MAX);
				flowPaths[flowPaths.Count - 1].flow = flow;
                maxFlow += flow;
            }
        }
		//Write results
		for(int i = 0; i < flowPaths.Count; i++)
		{
			if (flowPaths[i].flow == 0)
				continue;
			string path = "Sent "+ flowPaths[i].flow.ToString() + " energy over nodes";
			for (int j = 0; j < flowPaths[i].path.Count; j++)
				path += " " + flowPaths[i].path[j].ToString();
			Console.WriteLine(path);
		}

        result = maxFlow;
        return runCorrectly;
    }

    public long GetUsedCapacityOfNode(int nodeID)
    {
        List<Edge> edges = adj[nodeIDtoIndex[nodeID]];
        foreach (Edge edge in edges)
            if (edge.geomID == nodeID && edge.UsedCapacity != 0)//There are multiple with this geom ID
                return edge.UsedCapacity;
		//Shouldnt this just get usedcap of adj[0 to inoutoffset][0]?
        return 0;
    }

    public long GetUsedCapacityForCable(int cableID)
    {
		//Get the 2 edges for this connection
		Edge edge1 = cableIDtoEdge[cableID];
		Edge edge2 = adj[edge1.index - inOutOffset][edge1.rev];
		return Math.Max(edge1.UsedCapacity, edge2.UsedCapacity);
    }
}

class Edge
{
    public int index;   //Vertex index
    public long flow;    //Flow in edge
    public long cap;     //Capacity
    public int rev;     //Index of reverse edge
    public int geomID;
    public long startCap;

    public Edge(int index, long flow, long cap, int rev, int geomID)
    {
        this.index = index;
        this.flow = flow;
        this.cap = cap;
        this.rev = rev;
        this.geomID = geomID;
        startCap = cap;
    }

    public long UsedCapacity
    {
        get
        {
            //return Math.Abs(startCap - cap);
            return Math.Abs(flow);
        }
    }

	public bool HasCapacityLeft
	{
		get { return flow < cap; }
	}
}

class FlowPath
{
	public long flow;
	public List<int> path;
	public FlowPath()
	{
		path = new List<int>();
	}
}


