using System;

namespace MSW
{
	public static class ConsoleLogger
	{
		private static void WriteWithColor(string a_message, ConsoleColor a_color)
		{
			ConsoleColor orgColor = Console.ForegroundColor;
			Console.ForegroundColor = a_color;
			Console.WriteLine(a_message);
			Console.ForegroundColor = orgColor;
		}

		public static void Error(string a_message)
		{
			WriteWithColor(a_message, ConsoleColor.Red);
		}

		public static void Warning(string a_message)
		{
			WriteWithColor(a_message, ConsoleColor.Yellow);
		}

		public static void Info(string a_message)
		{
			Console.WriteLine(a_message);
		}
	}
}
