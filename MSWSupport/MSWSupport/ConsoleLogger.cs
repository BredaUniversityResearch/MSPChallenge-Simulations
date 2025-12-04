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
            if (context is Exception ex) {
                logEntry["context"] = SerializeException(ex);
            } else if (context != null) {
                logEntry["context"] = context;
            }
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
