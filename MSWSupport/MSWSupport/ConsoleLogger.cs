using System;
using System.Text.Json;
using System.Collections.Generic;

namespace MSWSupport
{
    public static class ConsoleLogger
    {
        public static void Write(string? aMessage)
        {
            Console.Write(aMessage);
        }        
        
        private static void WriteLine(string aMessage)
        {
            Console.WriteLine(aMessage!);
        }

        private static void WriteLineWithColor(string aMessage, ConsoleColor aColor)
        {
            var orgColor = Console.ForegroundColor;
            Console.ForegroundColor = aColor;
            Console.Error.WriteLine(aMessage!);
            Console.ForegroundColor = orgColor;
        }

        private static void WriteLineStructured(string message, string levelName, ConsoleColor color, object? context)
        {
            var logEntry = new Dictionary<string, object>
            {
                { "message", message },
                { "level_name", levelName }
            };
            var contextDict = new Dictionary<string, object>();
            switch (context)
            {
                case Dictionary<string, object> objects:
                    contextDict = objects;
                    break;
                case Exception ex:
                    contextDict.Add("exception", SerializeException(ex));
                    break;
                default:
                    if (context != null) contextDict.Add("context", context);
                    break;
            }
            var prefix = ConsoleTextWriter.Instance.GetMessageParameter("prefix");
            if (prefix != null)
            {
                logEntry["prefix"] = prefix;
            }
            logEntry.Add("context", contextDict);
            string json = JsonSerializer.Serialize(logEntry);
            WriteLineWithColor(json, color);
        }

        public static object SerializeException(Exception ex, bool isRoot = true)
        {
            var exceptionObj = new Dictionary<string, object>
            {
                { "exception", ex.Message }
            };

            if (isRoot && ex.StackTrace != null)
            {
                exceptionObj["stackTrace"] = ex.StackTrace;
            }

            if (ex.InnerException != null)
            {
                exceptionObj["innerException"] = SerializeException(ex.InnerException, false);
            }

            return exceptionObj;
        }

        public static void Error(string aMessage, object? context = null)
        {
            WriteLineStructured(aMessage, "ERROR", ConsoleColor.Red, context);
        }

        public static void Warning(string aMessage, object? context = null)
        {
            WriteLineStructured(aMessage, "WARNING", ConsoleColor.Yellow, context);
        }

        public static void Info(string aMessage)
        {
            WriteLine(aMessage);
        }
    }
}
