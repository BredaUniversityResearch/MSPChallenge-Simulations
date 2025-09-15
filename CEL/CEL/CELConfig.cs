using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using MSWSupport;
using Newtonsoft.Json;

class CELConfig
{
    private class JsonData
    {
        public string api_root = null;
	}

    private static CELConfig instance = null;
    public static CELConfig Instance
    {
        get
        {
            return instance;
        }
    }

    private JsonData settings = null;

    static CELConfig()
    {
        instance = new CELConfig();
    }

    private CELConfig()
    {
		try
		{
			string configString = File.ReadAllText("CEL_config.json", Encoding.UTF8);
			settings = JsonConvert.DeserializeObject<JsonData>(configString);
		}
		catch (Exception e)
		{
			ConsoleLogger.Error(e.Message);
		}

		if (CommandLineArguments.HasOptionValue("APIEndpoint"))
		{
			if (settings == null)
				settings = new JsonData();
			settings.api_root = CommandLineArguments.GetOptionValue("APIEndpoint");
		}

		if (settings == null)
			settings = new JsonData();
		if (settings.api_root == null)
        {
            settings.api_root = "http://localhost/dev/1/";
            ConsoleLogger.Info($"No configured API Endpoint found in the CEL_Config.json file, using default: {settings.api_root}");
        }

		// get session id from settings.api_root
		if (!int.TryParse(
			    Regex.Match(settings.api_root, @"\/(\d+)\/").Groups[1].Value,
			    out int sessionId
		    ))
		{
			throw new ArgumentException(
				$"settings.api_root '{settings.api_root}' does not contain a valid session ID number in the format '/<session id>/'"
			);
		}
		ConsoleTextWriter.Instance.SetMessageParameter("prefix", $"CEL{sessionId:D3}: ");
}

    public string APIRoot
    {
        get { return settings.api_root; }
    }
}

