using System;
using System.Collections.Generic;
using System.Net;
using System.Collections.Specialized;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;
using MSWSupport;


class EnergyDistribution : ITokenReceiver
{
	//Elements
	private Connection[] connectionObj;                 //<fromID, toID, cableID, cap>
	private List<InputNode> nodeObj;                    //<id, cap>     NO CABLES
	private int[] sourceObj;                            //<id>             
	private List<CountryGridSockets> socketCountryObj;  //<gridid, <countryid, cap, <socketid>>>

	private int intermediaryNodeIndex;                  //ID of the next intermediary node to be inserted
	private int numberOriginalNodes;                    //Number of nodes, not including main source & sink or intermediary nodes
	private bool firstUpdate = true;
	private int lastRunMonth;
	private Dinic dinic;

	private string currentApiAccessToken = "";
	private APITokenHandler tokenHandler;

	public EnergyDistribution()
	{
		tokenHandler = new APITokenHandler(this, CommandLineArguments.GetOptionValue(CommandLineArguments.MSWPipeName), "CEL", CELConfig.Instance.APIRoot);
	}

	public void Tick()
	{
		if (!CheckAPIAccess())
		{
			return;
		}

		if (firstUpdate)
		{
			lastRunMonth = GetCurrentMonth() - 1;
			firstUpdate = false;
		}

		int month = GetCurrentMonth();
		if (month > lastRunMonth)
		{
			if (GetIfUpdateNecessary())
			{
				bool successful = LoadData();
				if (successful)
				{
					using (PerformanceTimer timer = new PerformanceTimer(string.Format("Simulation run for month {0}\n", month)))
					{
						successful = RunDistribution();
					}
				}
				if (successful)
				{
					SubmitResults();
					lastRunMonth = month;
					SetLastRunMonth(lastRunMonth);
				}
				else
				{
					Console.WriteLine("ERROR\t| Energy update failed. Retrying...");
					dinic = null;
				}
			}
			else if (dinic == null)
			{
				//No update is required, but we just started and don't have data, so run anyway to submit new KPIs
				bool successful = LoadData();
				if (successful)
				{
					using (PerformanceTimer timer = new PerformanceTimer(string.Format("Initialised simulation at month {0}\n", month)))
					{
						successful = RunDistribution();
					}
				}
				if (successful)
				{
					SubmitResults();
					lastRunMonth = month;
					SetLastRunMonth(lastRunMonth);
				}
				else
				{
					Console.WriteLine("ERROR\t| Energy update failed. Retrying...");
					dinic = null;
				}
			}
			else
			{
				//No update is done. Existing data is used for KPIs of new month
				Console.WriteLine(string.Format("No update required for month {0}\n", month));
				SubmitKPIs();
				lastRunMonth = month;
				SetLastRunMonth(lastRunMonth);
			}

		}
	}

	private bool CheckAPIAccess()
	{
		APIRequest.Perform(CELConfig.Instance.APIRoot, "/api/security/checkaccess", currentApiAccessToken, null,
			out APIAccessResult result);
		return (result != null && result.status != APIAccessResult.EResult.Expired);
	}

	bool LoadData()
	{
		bool successful = true;
		var task1 = Task.Run(() =>
		{
			//connections <from, to, cable, cap>
			if (!APIRequest.Perform(CELConfig.Instance.APIRoot, "/api/cel/GetConnections", currentApiAccessToken, null,
				out connectionObj))
			{
				Console.WriteLine("\t| Data load failed.");
				successful = false;
			}
		});

		var task2 = Task.Run(() =>
	   {
		   //nodes <id, cap>   NO CABLES
		   if (!APIRequest.Perform(CELConfig.Instance.APIRoot, "/api/cel/GetNodes", currentApiAccessToken, null,
			   out nodeObj))
		   {
			   successful = false;
		   }
	   });

		var task3 = Task.Run(() =>
		{
			//source IDs <id>
			if (!APIRequest.Perform(CELConfig.Instance.APIRoot, "/api/cel/GetSources", currentApiAccessToken, null,
				out sourceObj))
			{
				successful = false;
			}
		});

		var task4 = Task.Run(() =>
		{
			//grid info <gridid, <country, cap, <socketid>>>
			if (!APIRequest.Perform(CELConfig.Instance.APIRoot, "/api/cel/GetGrids", currentApiAccessToken, null,
				out socketCountryObj))
			{
				successful = false;
			}
		});

		Task.WaitAll(task1, task2, task3, task4);
		if (successful)
			Console.WriteLine("\t| Data loaded successfully.");
		return successful;
	}

	bool RunDistribution()
	{
		List<Connection> initialEdges = new List<Connection>();
		numberOriginalNodes = nodeObj.Count;
		intermediaryNodeIndex = -3;

		//Add main source and sink to nodes
		nodeObj.Add(new InputNode(-1, Dinic.LONG_MAX));  //source
		nodeObj.Add(new InputNode(-2, Dinic.LONG_MAX));  //sink

		//Add intermediary nodes to simulate the energy required by countries in grids
		foreach (CountryGridSockets countryGridSockets in socketCountryObj)
		{
			foreach (CountrySockets countrySockets in countryGridSockets.energy)
			{
				nodeObj.Add(new InputNode(intermediaryNodeIndex, Dinic.LONG_MAX, countryGridSockets.grid, countrySockets.country));
				if (countrySockets.expected >= 0)
				{
					//Add intermediate node to sinks
					initialEdges.Add(new Connection(-2, intermediaryNodeIndex, 0, countrySockets.expected)); //Connection to main sink
					foreach (int socketID in countrySockets.sockets)
						initialEdges.Add(new Connection(intermediaryNodeIndex, socketID, 0, Dinic.LONG_MAX)); //Connections to sockets
				}
				else
				{
					//Add intermediary note to sources
					initialEdges.Add(new Connection(-1, intermediaryNodeIndex, 0, -countrySockets.expected)); //Connection to main source
					foreach (int socketID in countrySockets.sockets)
						initialEdges.Add(new Connection(intermediaryNodeIndex, socketID, 0, Dinic.LONG_MAX)); //Connections to sources
																											  //Maybe (intermediaryNodeIndex, socketID) need to be flipped. Argument 1 & 2
				}
				intermediaryNodeIndex--;
			}
		}

		//Create network and add nodes
		dinic = new Dinic(nodeObj, initialEdges);

		//Add cables
		foreach (Connection con in connectionObj)
			dinic.AddBidirectionalEdge(con.fromNodeID, con.toNodeID, con.maxcapacity, con.cableID);

		//Add connections from main source to sources
		foreach (int id in sourceObj)
		{
			//dinic.AddBidirectionalEdgeWithCapOfNode(-1, id, id, id, true);
			//The node is already limited by its capacity, no use limiting the edge aswell
			dinic.AddBidirectionalEdge(-1, id, Dinic.LONG_MAX, id, true);
		}

		long result = 0;
		if (dinic.DinicMaxflow(-1, -2, out result))
		{
			Console.WriteLine("\t| Total energy sent: " + result.ToString());
			return true;
		}
		else
		{
			Console.WriteLine("ERROR\t| The energy simulation encountered an error while running. Results were discarded.");
			return false;
		}
	}

	void SubmitResults()
	{
		List<GeomCapacityOutput> geomOutput = new List<GeomCapacityOutput>();
		//Submit used capacity of all nodes
		for (int i = 0; i < numberOriginalNodes; i++)
		{
			int ID = nodeObj[i].geometry_id;
			string usedCap = dinic.GetUsedCapacityOfNode(ID).ToString("D");
			geomOutput.Add(new GeomCapacityOutput(ID, usedCap));
		}
		//Submit used capacity of all cables
		foreach (Connection con in connectionObj)
		{
			int ID = con.cableID;
			string usedCap = dinic.GetUsedCapacityForCable(ID).ToString("D");
			geomOutput.Add(new GeomCapacityOutput(ID, usedCap));
		}

		NameValueCollection nvc = new NameValueCollection();
		nvc.Add("geomCapacityValues", JsonConvert.SerializeObject(geomOutput));
		APIRequest.Perform(CELConfig.Instance.APIRoot, "/api/cel/SetGeomCapacity", currentApiAccessToken, nvc);

		SubmitKPIs();
	}

	void SubmitKPIs()
	{
		List<KPIOutput> geomOutput = new List<KPIOutput>();

		//Submit grid results per country
		int nodeStartIndex = numberOriginalNodes + 2;//Go through the grids' intermediary nodes
		for (int i = nodeStartIndex; i < nodeObj.Count; i++)
		{
			geomOutput.Add(new KPIOutput(nodeObj[i].grid,
				dinic.GetUsedCapacityOfNode(nodeObj[i].geometry_id).ToString("D"),
				nodeObj[i].country));
		}

		NameValueCollection nvc = new NameValueCollection();
		nvc.Add("kpiValues", JsonConvert.SerializeObject(geomOutput));
		APIRequest.Perform(CELConfig.Instance.APIRoot, "/api/cel/SetGridCapacity", currentApiAccessToken, nvc);
	}

	private int GetCurrentMonth()
	{
		if (APIRequest.Perform(CELConfig.Instance.APIRoot, "/api/Game/GetCurrentMonth",
			currentApiAccessToken, null, out MonthContainer result))
		{
			return result.game_currentmonth;
		}

		return -1;
	}

	private void SetLastRunMonth(int newMonth)
	{
		NameValueCollection form = new NameValueCollection();
		form.Add("month", newMonth.ToString());
		APIRequest.Perform(CELConfig.Instance.APIRoot, "/api/cel/UpdateFinished", currentApiAccessToken, form);
	}

	private bool GetIfUpdateNecessary()
	{
		bool success = APIRequest.Perform(CELConfig.Instance.APIRoot, "/api/cel/ShouldUpdate", currentApiAccessToken, null, out bool result);
		return success && result;
	}

	public void UpdateAccessToken(string newAccessToken)
	{
		currentApiAccessToken = newAccessToken;
	}
}

#region DataContainers

class Connection
{
	public int fromNodeID;
	public int toNodeID;
	public int cableID;
	public long maxcapacity;

	public Connection(int from, int to, int cable, long capacity)
	{
		fromNodeID = from;
		toNodeID = to;
		cableID = cable;
		this.maxcapacity = capacity;
	}
}

class InputNode
{
	public int geometry_id;
	public long maxcapacity;
	public int country;     //These are only set for intermediary nodes generated by sockets
	public int grid;        //And are only required for the output of KPIs

	public InputNode(int ID, long capacity, int grid = 0, int country = 0)
	{
		this.geometry_id = ID;
		this.maxcapacity = capacity;
		this.country = country;
		this.grid = grid;
	}
}

class CountryGridSockets
{
	public List<CountrySockets> energy;
	public int grid;

	public CountryGridSockets(List<CountrySockets> countrySockets, int gridid)
	{
		this.energy = countrySockets;
		this.grid = gridid;
	}
}

class CountrySockets
{
	public List<int> sockets;
	public long expected;
	public int country;

	public CountrySockets(List<int> sockets, long expectedCapacity, int countryid)
	{
		this.sockets = sockets;
		this.expected = expectedCapacity;
		this.country = countryid;
	}
}

class MonthContainer
{
	public int game_currentmonth = -1;
}

class KPIOutput
{
	public int grid;
	public string actual;
	public int country;

	public KPIOutput(int grid, string actual, int country)
	{
		this.grid = grid;
		this.actual = actual;
		this.country = country;
	}
}

class GeomCapacityOutput
{
	public int id;
	public string capacity;

	public GeomCapacityOutput(int id, string capacity)
	{
		this.id = id;
		this.capacity = capacity;
	}
}

#endregion