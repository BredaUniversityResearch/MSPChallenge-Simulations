using System;
using System.Collections.Generic;
using System.Linq;

public class CommandLineArgumentsManager
{
	public static CommandLineArgumentsManager Instance = new CommandLineArgumentsManager();
	private Dictionary<string, string> m_commandLineArguments = new Dictionary<string, string>();
	
	public enum CommandLineArgumentName
	{
		Port
	}

	private CommandLineArgumentsManager()
	{
		var allowedNames = Enum.GetNames(typeof(CommandLineArgumentName));
		var args = Environment.GetCommandLineArgs();
		for (var n = 1; n < args.Length; n++)
		{
			var arg = args[n];
			var parts = arg.Split('=');
			if (parts.Length != 2) continue;
			if (!allowedNames.Contains(parts[0])) continue;
			m_commandLineArguments[parts[0]] = parts[1];
		}			
	}
	
	public string GetCommandLineArgumentValue(CommandLineArgumentName name)
	{
		return !m_commandLineArguments.ContainsKey(name.ToString()) ? null : m_commandLineArguments[name.ToString()];
	}		
	
	public string AutoFill(CommandLineArgumentName commandLineArgumentName, string defaultValue)
	{
		var autoFillValue = GetCommandLineArgumentValue(commandLineArgumentName);
		return autoFillValue ?? defaultValue;
	}		
}