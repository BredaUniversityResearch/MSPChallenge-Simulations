using System;
using System.Collections.Generic;

/// <summary>
/// Utility class which contains information passed in the commandline.
/// This should allow us to run with commandlines such as "SomeFlag OptionA ValueA FlagB" which toggles SomeFlag and FlagB on and sets OptionA to ValueA
/// 
/// Copied from SEL
/// </summary>
public static class CommandLineArguments
{
	public const string APIEndpoint = "APIEndpoint";
	public const string MSWPipeName = "MSWPipe";

	public static HashSet<string> ms_ValueOptions = new HashSet<string>(); //The options that should have a value associated with them
	public static Dictionary<string, string> ms_OptionValueTable = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

	static CommandLineArguments()
	{
		ms_ValueOptions.Add(APIEndpoint);
		ms_ValueOptions.Add(MSWPipeName);

		ParseCommandLine(ms_OptionValueTable);
	}

	private static void ParseCommandLine(Dictionary<string, string> outputTable)
	{
		string[] arguments = Environment.GetCommandLineArgs();

		foreach (string arg in arguments)
		{
			foreach (string optionName in ms_ValueOptions)
			{
				if (arg.StartsWith(optionName) && arg[optionName.Length] == '=')
				{
					outputTable.Add(optionName, arg.Substring(optionName.Length + 1));
					break;
				}
			}
		}
	}

	public static bool HasOptionValue(string optionName)
	{
		string result;
		ms_OptionValueTable.TryGetValue(optionName, out result);
		return result != null;
	}

	public static string GetOptionValue(string optionName)
	{
		string result;
		ms_OptionValueTable.TryGetValue(optionName, out result);
		return result;
	}
}

