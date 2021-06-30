using System;
using System.Diagnostics;

public class PerformanceTimer : IDisposable
{
    private string message;
    private Stopwatch stopWatch = new Stopwatch();

    public PerformanceTimer(string Message)
    {
        message = Message;
        stopWatch.Start();
    }

    public void Dispose()
    {
        stopWatch.Stop();
        Console.WriteLine(string.Format("{0}ms\t| {1} ", stopWatch.ElapsedMilliseconds, message));
    }
}

