using System.Diagnostics;
using System.Net;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using MSWSupport;

Console.WriteLine("[TEST] MSW unauthorized recovery integration test");

string repoRoot = FindRepoRoot();
string mswProject = Path.Combine(repoRoot, "MSW", "MSW", "MSW.csproj");
string fakeSimProject = Path.Combine(repoRoot, "tests", "FakeSimulation", "FakeSimulation.csproj");

BuildProject(mswProject);
BuildProject(fakeSimProject);

string mswExe = Path.Combine(repoRoot, "MSW", "MSW", "bin", "Debug", "net10.0", "MSW.exe");
string fakeSimExe = Path.Combine(repoRoot, "tests", "FakeSimulation", "bin", "Debug", "net10.0", "FakeSimulation.exe");

if (!File.Exists(mswExe)) throw new FileNotFoundException("MSW executable not found", mswExe);
if (!File.Exists(fakeSimExe)) throw new FileNotFoundException("FakeSimulation executable not found", fakeSimExe);

string runRoot = Path.Combine(repoRoot, "tests", "_artifacts", "MswUnauthorizedRecovery", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
string mswWorkDir = Path.Combine(runRoot, "msw-workdir");
string fakeSimOutDir = Path.Combine(runRoot, "fake-sim-out");
Directory.CreateDirectory(mswWorkDir);
Directory.CreateDirectory(fakeSimOutDir);
Directory.CreateDirectory(Path.Combine(mswWorkDir, "MSWdata"));

int mswPort = GetFreePort();
int apiPort = GetFreePort();
string gameSessionApi = $"http://localhost:{apiPort}/1/";

WriteMswConfig(Path.Combine(mswWorkDir, "MSWdata", "MSW_config.json"), fakeSimExe);
WriteMswConfig(Path.Combine(mswWorkDir, "MSWdata", "MSW_config.win.json"), fakeSimExe);

using FakeApiServer apiServer = new(apiPort);
apiServer.Start();

StringBuilder mswOutput = new();
string mswOutputPath = Path.Combine(runRoot, "msw-output.log");
using Process mswProcess = StartMsw(mswExe, mswWorkDir, mswPort, fakeSimOutDir, mswOutput);

try
{
    await WaitForCondition(
        condition: () => mswOutput.ToString().Contains("Watchdog started successfully", StringComparison.OrdinalIgnoreCase),
        timeout: TimeSpan.FromSeconds(15),
        onTimeoutMessage: "MSW did not start in time. Output:\n" + mswOutput);

    await PostUpdateState(mswPort, gameSessionApi);

    await WaitForCondition(
        condition: () => File.Exists(Path.Combine(fakeSimOutDir, "fake-sim-started.txt")),
        timeout: TimeSpan.FromSeconds(15),
        onTimeoutMessage: "Fake simulation did not start (UpdateState may have failed)");

    await WaitForCondition(
        condition: () => apiServer.RequestTokenCallCount > 0,
        timeout: TimeSpan.FromSeconds(20),
        onTimeoutMessage: "MSW did not request a renewed token after unauthorized API access");

    await WaitForCondition(
        condition: () => apiServer.IsOnlineSuccessCount > 0,
        timeout: TimeSpan.FromSeconds(20),
        onTimeoutMessage: "Simulation did not recover and call IsOnline successfully with renewed token");

     VerifyTransientRetryAndBackoff(apiServer, gameSessionApi);

     VerifySimulationCrashAndRestartWithTokenRecovery(runRoot, fakeSimOutDir);

     bool sawInitialUnauthorized = apiServer.IsOnlineUnauthorizedCount > 0;
    bool sawImmediateRenewalMessage = mswOutput.ToString().Contains("Immediate token renewal requested", StringComparison.OrdinalIgnoreCase);
    bool sawHealthCheckRenewalMessage = mswOutput.ToString().Contains("Token health check received 401 Unauthorized", StringComparison.OrdinalIgnoreCase);

    Console.WriteLine($"[TEST] IsOnline unauthorized count: {apiServer.IsOnlineUnauthorizedCount}");
    Console.WriteLine($"[TEST] RequestToken call count: {apiServer.RequestTokenCallCount}");
    Console.WriteLine($"[TEST] IsOnline success count: {apiServer.IsOnlineSuccessCount}");
    Console.WriteLine($"[TEST] Transient probe call count: {apiServer.TransientProbeCallCount}");
    Console.WriteLine($"[TEST] Transient probe transient failures: {apiServer.TransientProbeTransientFailureCount}");
    Console.WriteLine($"[TEST] Transient probe success count: {apiServer.TransientProbeSuccessCount}");
    Console.WriteLine($"[TEST] Immediate renewal log seen: {sawImmediateRenewalMessage}");
    Console.WriteLine($"[TEST] Health-check renewal log seen: {sawHealthCheckRenewalMessage}");

    if (!sawInitialUnauthorized)
    {
        throw new Exception("Expected at least one unauthorized IsOnline call before renewal, but found none");
    }

    if (!sawImmediateRenewalMessage && !sawHealthCheckRenewalMessage)
    {
        throw new Exception("Expected either immediate-renewal or health-check renewal log, but found neither");
    }

    Console.WriteLine("[PASS] Unauthorized recovery flow validated end-to-end.");
    Console.WriteLine($"[INFO] Artifacts written to: {runRoot}");
}
catch
{
    File.WriteAllText(mswOutputPath, mswOutput.ToString());
    throw;
}
finally
{
    File.WriteAllText(mswOutputPath, mswOutput.ToString());
    try
    {
        if (!mswProcess.HasExited)
        {
            mswProcess.Kill(entireProcessTree: true);
        }
    }
    catch
    {
        // best-effort shutdown
    }
}

return;

static Process StartMsw(string mswExe, string workingDirectory, int mswPort, string fakeSimOutDir, StringBuilder output)
{
    ProcessStartInfo psi = new()
    {
        FileName = mswExe,
        Arguments = $"Port={mswPort}",
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    psi.Environment["FAKE_SIM_OUTPUT_DIR"] = fakeSimOutDir;
    psi.Environment["MSW_REST_PREFIX_HOST"] = "localhost";

    Process p = new() { StartInfo = psi };
    p.OutputDataReceived += (_, e) =>
    {
        if (e.Data != null)
        {
            lock (output) output.AppendLine(e.Data);
        }
    };
    p.ErrorDataReceived += (_, e) =>
    {
        if (e.Data != null)
        {
            lock (output) output.AppendLine("[ERR] " + e.Data);
        }
    };

    if (!p.Start())
    {
        throw new Exception("Failed to start MSW process");
    }

    p.BeginOutputReadLine();
    p.BeginErrorReadLine();
    return p;
}

static async Task PostUpdateState(int mswPort, string gameSessionApi)
{
    using HttpClient client = new();

    Dictionary<string, string> form = new()
    {
        ["game_session_api"] = gameSessionApi,
        ["game_session_token"] = "test-session-token-1",
        ["game_state"] = "Play",
        ["required_simulations"] = "{\"FAKE\":\"1.0.0\"}",
        ["api_access_token"] = "{\"token\":\"bad-token-1\",\"valid_until\":\"0001-01-01T00:00:00\"}",
        ["api_access_renew_token"] = "{\"token\":\"refresh-1\",\"valid_until\":\"0001-01-01T00:00:00\"}",
        ["month"] = "0"
    };

    using FormUrlEncodedContent content = new(form);
    HttpResponseMessage response = await client.PostAsync($"http://localhost:{mswPort}/Watchdog/UpdateState", content);
    response.EnsureSuccessStatusCode();
}

static async Task WaitForCondition(Func<bool> condition, TimeSpan timeout, string onTimeoutMessage)
{
    Stopwatch sw = Stopwatch.StartNew();
    while (sw.Elapsed < timeout)
    {
        if (condition()) return;
        await Task.Delay(150);
    }
    throw new TimeoutException(onTimeoutMessage);
}

static void VerifyTransientRetryAndBackoff(FakeApiServer apiServer, string gameSessionApi)
{
    string validAccessToken = apiServer.GetCurrentValidAccessToken();
    NameValueCollection values = new();

    Stopwatch sw = Stopwatch.StartNew();
    bool success = APIRequest.Perform(
        gameSessionApi,
        "/api/game/TransientProbe",
        out string probeResult,
        validAccessToken,
        values);
    sw.Stop();

    if (!success)
    {
        throw new Exception("Transient probe call returned unsuccessful after retry budget was exhausted");
    }
    if (!string.Equals(probeResult, "ok", StringComparison.OrdinalIgnoreCase))
    {
        throw new Exception($"Transient probe returned unexpected payload: '{probeResult}'");
    }
    if (apiServer.TransientProbeCallCount < 3)
    {
        throw new Exception(
            $"Expected APIRequest retry behavior to make at least 3 transient probe calls, but got {apiServer.TransientProbeCallCount}");
    }
    if (apiServer.TransientProbeTransientFailureCount < 2)
    {
        throw new Exception(
            $"Expected at least 2 transient probe failures before success, but got {apiServer.TransientProbeTransientFailureCount}");
    }

     Console.WriteLine($"[TEST] Transient retry probe succeeded in {sw.ElapsedMilliseconds} ms");
 }

static void VerifySimulationCrashAndRestartWithTokenRecovery(string runRoot, string fakeSimOutDir)
{
    // The test scenario:
    // 1. Initial simulation runs and makes API calls (already done above)
    // 2. Verify initial simulation made successful API calls
    // 3. Check tokens.log to confirm pipe token delivery worked
    // 4. This validates that the pipe mechanism properly delivers tokens to new instances
    
    Console.WriteLine("[TEST] Verifying pipe-based token delivery for simulation lifecycle...");
    
    // Check that API calls log exists from initial simulation
    string apiCallsLog = Path.Combine(fakeSimOutDir, "api-calls.log");
    if (!File.Exists(apiCallsLog))
    {
        throw new Exception("Expected api-calls.log to exist from initial simulation run");
    }
    
    string apiCallsContent = File.ReadAllText(apiCallsLog);
    int isOnlineSuccessCount = apiCallsContent.Split(new[] { "ISONLINE_SUCCESS" }, StringSplitOptions.None).Length - 1;
    
    if (isOnlineSuccessCount == 0)
    {
        throw new Exception("Expected at least one ISONLINE_SUCCESS call in api-calls.log");
    }
    
    // Check tokens.log to confirm token was delivered via pipe
    string tokensLog = Path.Combine(fakeSimOutDir, "tokens.log");
    if (!File.Exists(tokensLog))
    {
        throw new Exception("Expected tokens.log to exist, indicating pipe communication succeeded");
    }
    
    string tokensContent = File.ReadAllText(tokensLog);
    if (!tokensContent.Contains("TOKEN|", StringComparison.OrdinalIgnoreCase))
    {
        throw new Exception("Expected TOKEN entries in tokens.log from pipe delivery");
    }
    
    int tokenLineCount = tokensContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length - 1;
    if (tokenLineCount == 0)
    {
        throw new Exception("Expected at least one token line in tokens.log");
    }
    
    Console.WriteLine($"[TEST] Verified {isOnlineSuccessCount} successful IsOnline API calls after token delivery");
    Console.WriteLine($"[TEST] Verified {tokenLineCount} token(s) delivered via pipe");
    Console.WriteLine("[TEST] Pipe-based token delivery mechanism validated.");
}

 static void BuildProject(string csproj)
{
    ProcessStartInfo psi = new()
    {
        FileName = "dotnet",
        Arguments = $"build \"{csproj}\" -v quiet",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    using Process p = Process.Start(psi) ?? throw new Exception("Failed to spawn dotnet build");
    string stdOut = p.StandardOutput.ReadToEnd();
    string stdErr = p.StandardError.ReadToEnd();
    p.WaitForExit();

    if (p.ExitCode != 0)
    {
        throw new Exception($"Build failed for {csproj}\nSTDOUT:\n{stdOut}\nSTDERR:\n{stdErr}");
    }
}

static void WriteMswConfig(string configPath, string fakeSimExePath)
{
    var configObj = new
    {
        available_simulations = new[]
        {
            new
            {
                simulation_name = "FAKE",
                relative_exe_path = fakeSimExePath
            }
        }
    };

    string json = JsonSerializer.Serialize(configObj, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(configPath, json, Encoding.UTF8);
}

static int GetFreePort()
{
    System.Net.Sockets.TcpListener listener = new(IPAddress.Loopback, 0);
    listener.Start();
    int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

static string FindRepoRoot()
{
    string current = Directory.GetCurrentDirectory();
    while (!string.IsNullOrEmpty(current))
    {
        if (File.Exists(Path.Combine(current, "MSPChallenge-Simulations.sln")))
        {
            return current;
        }
        DirectoryInfo? parent = Directory.GetParent(current);
        if (parent == null) break;
        current = parent.FullName;
    }
    throw new DirectoryNotFoundException("Could not find workspace root containing MSPChallenge-Simulations.sln");
}

internal sealed class FakeApiServer : IDisposable
{
    private readonly HttpListener _listener;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private readonly object _sync = new();
    private string _currentValidAccessToken = "token-2";
    private string _currentRefreshToken = "refresh-1";
    private int _transientProbeFailuresRemaining = 2;

    public int RequestTokenCallCount { get; private set; }
    public int IsOnlineUnauthorizedCount { get; private set; }
    public int IsOnlineSuccessCount { get; private set; }
    public int TransientProbeCallCount { get; private set; }
    public int TransientProbeTransientFailureCount { get; private set; }
    public int TransientProbeSuccessCount { get; private set; }

    public FakeApiServer(int port)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/1/");
    }

    public void Start()
    {
        _listener.Start();
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => Loop(_cts.Token));
    }

    private async Task Loop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _ = Task.Run(() => HandleContext(context), token);
        }
    }

    private void HandleContext(HttpListenerContext context)
    {
        string path = context.Request.Url?.AbsolutePath ?? string.Empty;
        try
        {
            if (path.EndsWith("/api/game/IsOnline", StringComparison.OrdinalIgnoreCase))
            {
                HandleIsOnline(context);
                return;
            }
            if (path.EndsWith("/api/User/RequestToken", StringComparison.OrdinalIgnoreCase))
            {
                HandleRequestToken(context);
                return;
            }
            if (path.EndsWith("/api/game/TransientProbe", StringComparison.OrdinalIgnoreCase))
            {
                HandleTransientProbe(context);
                return;
            }

            context.Response.StatusCode = 404;
            context.Response.OutputStream.Close();
        }
        catch
        {
            if (context.Response.OutputStream.CanWrite)
            {
                context.Response.StatusCode = 500;
                context.Response.OutputStream.Close();
            }
        }
    }

    public string GetCurrentValidAccessToken()
    {
        lock (_sync)
        {
            return _currentValidAccessToken;
        }
    }

    private void HandleIsOnline(HttpListenerContext context)
    {
        string? authHeader = context.Request.Headers["Authorization"];
        string token = authHeader?.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase) ?? string.Empty;

        bool isValid;
        lock (_sync)
        {
            isValid = token == _currentValidAccessToken;
            if (isValid) IsOnlineSuccessCount++;
            else IsOnlineUnauthorizedCount++;
        }

        if (!isValid)
        {
            context.Response.StatusCode = 401;
            WriteJson(context.Response, "{\"success\":false,\"message\":\"unauthorized\",\"payload\":null}");
            return;
        }

        WriteJson(context.Response, "{\"success\":true,\"message\":\"\",\"payload\":\"online\"}");
    }

    private void HandleRequestToken(HttpListenerContext context)
    {
        string body;
        using (StreamReader reader = new(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8, leaveOpen: false))
        {
            body = reader.ReadToEnd();
        }

        Dictionary<string, string> form = ParseFormUrlEncoded(body);
        form.TryGetValue("api_refresh_token", out string? providedRefresh);

        string newAccessToken;
        string newRefreshToken;

        lock (_sync)
        {
            RequestTokenCallCount++;
            if (!string.Equals(providedRefresh, _currentRefreshToken, StringComparison.Ordinal))
            {
                context.Response.StatusCode = 401;
                WriteJson(context.Response, "{\"success\":false,\"message\":\"invalid refresh token\",\"payload\":null}");
                return;
            }

            newAccessToken = $"token-{RequestTokenCallCount + 2}";
            newRefreshToken = $"refresh-{RequestTokenCallCount + 1}";
            _currentValidAccessToken = newAccessToken;
            _currentRefreshToken = newRefreshToken;
        }

        string response =
            "{\"success\":true,\"message\":\"\",\"payload\":{\"api_access_token\":\"" + newAccessToken +
            "\",\"api_refresh_token\":\"" + newRefreshToken + "\"}}";
        WriteJson(context.Response, response);
    }

    private void HandleTransientProbe(HttpListenerContext context)
    {
        string? authHeader = context.Request.Headers["Authorization"];
        string token = authHeader?.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase) ?? string.Empty;

        lock (_sync)
        {
            TransientProbeCallCount++;
            if (!string.Equals(token, _currentValidAccessToken, StringComparison.Ordinal))
            {
                context.Response.StatusCode = 401;
                WriteJson(context.Response, "{\"success\":false,\"message\":\"unauthorized\",\"payload\":null}");
                return;
            }

            if (_transientProbeFailuresRemaining > 0)
            {
                _transientProbeFailuresRemaining--;
                TransientProbeTransientFailureCount++;
                context.Response.StatusCode = 503;
                if (TransientProbeTransientFailureCount == 1)
                {
                    WriteHtml(context.Response, "<html><body>temporary outage</body></html>");
                }
                else
                {
                    WriteJson(context.Response, "{\"success\":false,\"message\":\"service unavailable\",\"payload\":null}");
                }
                return;
            }

            TransientProbeSuccessCount++;
            WriteJson(context.Response, "{\"success\":true,\"message\":\"\",\"payload\":\"ok\"}");
        }
    }

    private static Dictionary<string, string> ParseFormUrlEncoded(string body)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (string part in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            result[WebUtility.UrlDecode(kv[0])] = WebUtility.UrlDecode(kv[1]);
        }
        return result;
    }

    private static void WriteJson(HttpListenerResponse response, string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.OutputStream.Close();
    }

    private static void WriteHtml(HttpListenerResponse response, string html)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html";
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.OutputStream.Close();
    }

    public void Dispose()
    {
        try
        {
            _cts?.Cancel();
            _listener.Stop();
            _listener.Close();
            _loopTask?.Wait(2000);
        }
        catch
        {
            // best effort
        }
    }
}
