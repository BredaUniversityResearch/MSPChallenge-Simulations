using System;
using System.Diagnostics;
using MSWSupport;

namespace MEL;

public class CustomConsoleTraceListener : TraceListener
{
    public override void Write(string? message)
    {
        ConsoleLogger.Write(message);
    }

    public override void WriteLine(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            Console.WriteLine(message);
            return;
        }
        if ((message.Contains("error", StringComparison.OrdinalIgnoreCase) ||
             message.Contains("exception", StringComparison.OrdinalIgnoreCase)))
        {
            ConsoleLogger.Warning(message!);
            return;
        }
        ConsoleLogger.Info(message!);
    }
}
