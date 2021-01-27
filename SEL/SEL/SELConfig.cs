using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace SEL
{
	public class SELConfig
	{
		private class JsonData
		{
			public string api_root = null;
			public int route_finder_parallel_tasks = 2;
			public bool create_edge_map = false;
			public int edge_map_resolution = 16384;
			public bool create_route_maps = false;
			public int route_map_resolution = 4096;
			public bool enable_routefinder_debug = false;
			public bool enable_routefinder_debug_failed_routes = false;
			public int routefinder_output_resolution = 4096;
			public bool should_output_restriction_maps = false;
			public bool ignore_security_tokens = false;
			public bool upload_route_graph_data = false;
		}

		public static SELConfig Instance
		{
			get;
		}

		private JsonData m_settings = null;

		static SELConfig()
		{
			Instance = new SELConfig();
		}

		private SELConfig()
		{
			string configString = File.ReadAllText("SEL_config.json", Encoding.UTF8);
			m_settings = JsonConvert.DeserializeObject<JsonData>(configString);

			//Try and get the api_root from the commandline. If nothing on the commandline is specified, use the config file value. If neither are specified use a fallback.
			if (CommandLineArguments.HasOptionValue("APIEndpoint"))
			{
				m_settings.api_root = CommandLineArguments.GetOptionValue("APIEndpoint");
			}
			else if (m_settings.api_root == null)
			{
				m_settings.api_root = "http://localhost/dev/";
				Console.WriteLine("No configured API Endpoint found either in the SEL_Config.json file or on the APIEndpoint commandline argument, using default {0}", m_settings.api_root);
			}
		}

		public string GetAPIRoot()
		{
			return m_settings.api_root;
		}

		public bool ShouldCreateEdgeMap()
		{
			return m_settings.create_edge_map;
		}

		public int EdgeMapOutputResolution()
		{
			return m_settings.edge_map_resolution;
		}

		public bool ShouldCreateRouteMaps()
		{
			return m_settings.create_route_maps;
		}

		public int RouteMapOutputResolution()
		{
			return m_settings.route_map_resolution;
		}

		public bool IsRouteFinderDebugEnabled()
		{
			return m_settings.enable_routefinder_debug;
		}

		public bool ShouldOutputFailedRoutes()
		{
			return m_settings.enable_routefinder_debug_failed_routes;
		}

		public int RouteFinderOutputResolution()
		{
			return m_settings.routefinder_output_resolution;
		}

		public int RouteFinderParallelTasks()
		{
			return m_settings.route_finder_parallel_tasks;
		}

		public bool ShouldOutputRestrictionMaps()
		{
			return m_settings.should_output_restriction_maps;
		}

		public bool ShouldIgnoreApiSecurity()
		{
			return m_settings.ignore_security_tokens;
		}

		public bool ShouldUploadRouteGraphData()
		{
			return m_settings.upload_route_graph_data;
		}
	}
}
