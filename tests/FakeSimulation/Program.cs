using System.Collections.Specialized;
using System.IO.Pipes;
using System.Text;
using MSWSupport;

const int RunSeconds = 25;

string? pipeName = null;
string? apiEndpoint = null;
string? outputDir = Environment.GetEnvironmentVariable("FAKE_SIM_OUTPUT_DIR");
string? crashAfterSecondsStr = Environment.GetEnvironmentVariable("FAKE_SIM_CRASH_AFTER_SECONDS");
int crashAfterSeconds = int.TryParse(crashAfterSecondsStr, out int crash) ? crash : -1;

foreach (string arg in Environment.GetCommandLineArgs())
{
    if (arg.StartsWith("MSWPipe=", StringComparison.OrdinalIgnoreCase))
    {
        pipeName = arg.Substring("MSWPipe=".Length);
    }
    else if (arg.StartsWith("APIEndpoint=", StringComparison.OrdinalIgnoreCase))
    {
        apiEndpoint = arg.Substring("APIEndpoint=".Length);
    }
}

if (string.IsNullOrWhiteSpace(pipeName))
{
    throw new InvalidOperationException("MSWPipe argument missing");
}
if (string.IsNullOrWhiteSpace(apiEndpoint))
{
    throw new InvalidOperationException("APIEndpoint argument missing");
}
if (string.IsNullOrWhiteSpace(outputDir))
{
    throw new InvalidOperationException("FAKE_SIM_OUTPUT_DIR environment variable missing");
}

Directory.CreateDirectory(outputDir);
File.WriteAllText(Path.Combine(outputDir, "fake-sim-started.txt"), DateTime.UtcNow.ToString("O"));

MswClientNotifier.Initialize();

string accessToken = string.Empty;
DateTime startedAt = DateTime.UtcNow;

using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.In);
pipe.Connect(10000);

using StreamReader reader = new(pipe, Encoding.UTF8, false, 128, true);
using CancellationTokenSource readCts = new();

Task readTask = Task.Run(() =>
{
    while (!readCts.Token.IsCancellationRequested)
    {
        string? line = reader.ReadLine();
        if (line == null)
        {
            Thread.Sleep(50);
            continue;
        }

        File.AppendAllLines(Path.Combine(outputDir, "pipe-lines.log"),
        [
            $"{DateTime.UtcNow:O}|{line}"
        ]);

        string normalizedLine = line.TrimStart('\uFEFF');
        if (!normalizedLine.StartsWith(CommunicationPipeHandler.TOKEN_PRELUDE, StringComparison.Ordinal))
        {
            continue;
        }

        accessToken = normalizedLine.Substring(CommunicationPipeHandler.TOKEN_PRELUDE.Length);
        File.AppendAllLines(Path.Combine(outputDir, "tokens.log"),
        [
            $"{DateTime.UtcNow:O}|TOKEN|{accessToken}"
        ]);
    }
}, readCts.Token);

while ((DateTime.UtcNow - startedAt).TotalSeconds < RunSeconds)
{
    if (crashAfterSeconds > 0 && (DateTime.UtcNow - startedAt).TotalSeconds >= crashAfterSeconds)
    {
        File.AppendAllLines(Path.Combine(outputDir, "api-calls.log"),
        [
            $"{DateTime.UtcNow:O}|CRASH|Simulated crash triggered after {crashAfterSeconds} seconds"
        ]);
        throw new Exception("Simulated background task failure for testing restart recovery");
    }

    if (!string.IsNullOrWhiteSpace(accessToken))
    {
        try
        {
            bool success = APIRequest.Perform(
                apiEndpoint,
                "api/game/IsOnline",
                out string isOnline,
                accessToken,
                new NameValueCollection());
            if (success)
            {
                File.AppendAllLines(Path.Combine(outputDir, "api-calls.log"),
                [
                    $"{DateTime.UtcNow:O}|ISONLINE_SUCCESS|{isOnline}|token-prefix={accessToken[..Math.Min(accessToken.Length, 12)]}"
                ]);
            }
        }
        catch (ApiUnauthorizedWebException)
        {
            File.AppendAllLines(Path.Combine(outputDir, "api-calls.log"),
            [
                $"{DateTime.UtcNow:O}|ISONLINE_401|token-prefix={accessToken[..Math.Min(accessToken.Length, 12)]}"
            ]);
        }
    }

    Thread.Sleep(300);
}

readCts.Cancel();
File.WriteAllText(Path.Combine(outputDir, "fake-sim-finished.txt"), DateTime.UtcNow.ToString("O"));
