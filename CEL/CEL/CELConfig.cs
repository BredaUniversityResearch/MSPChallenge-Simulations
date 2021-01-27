using System;
using System.Text;
using System.IO;
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
			Console.WriteLine(e.Message);
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
            Console.WriteLine(string.Format("No configured API Endpoint found in the CEL_Config.json file, using default: {0}", settings.api_root));
        }
    }

    public string APIRoot
    {
        get { return settings.api_root; }
    }
}

