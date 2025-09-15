using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MSWSupport;

namespace REL
{
	public class RELConfig
	{
		private class JsonData
		{
			public string api_root = null;
		}

		public static RELConfig Instance
		{
			get;
		}

		private JsonData m_settings = null;

		static RELConfig()
		{
			Instance = new RELConfig();
		}

		private RELConfig()
		{
			string configString = File.ReadAllText("REL_config.json", Encoding.UTF8);
			m_settings = JsonConvert.DeserializeObject<JsonData>(configString);

			//Try and get the api_root from the commandline. If nothing on the commandline is specified, use the config file value. If neither are specified use a fallback.
			if (CommandLineArguments.HasOptionValue("APIEndpoint"))
			{
				m_settings.api_root = CommandLineArguments.GetOptionValue("APIEndpoint");
			}
			else if (m_settings.api_root == null)
			{
				m_settings.api_root = "http://localhost/1/";
				Console.WriteLine("No configured API Endpoint found either in the SEL_Config.json file or on the APIEndpoint commandline argument, using default {0}", m_settings.api_root);
			}
			// get session id from settings.api_root
			if (!int.TryParse(
				    Regex.Match(m_settings.api_root, @"\/(\d+)\/").Groups[1].Value,
				    out int sessionId
			    ))
			{
				throw new ArgumentException(
					$"m_settings.api_root '{m_settings.api_root}' does not contain a valid session ID number in the format '/<session id>/'"
				);
			}
			ConsoleTextWriter.Instance.SetMessageParameter("prefix", $"REL{sessionId:D3}: ");
		}

		public string GetAPIRoot()
		{
			return m_settings.api_root;
		}
	}
}
