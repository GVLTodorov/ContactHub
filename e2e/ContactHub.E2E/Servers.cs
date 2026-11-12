using System.Diagnostics;

namespace ContactHub.E2E;

/// <summary>
/// Starts (or reuses) the backend and frontend dev servers. Linked into ContactHub.Demo.
/// </summary>
internal sealed class Servers : IDisposable
{
    public const string BackendUrl = "http://127.0.0.1:8080/api/contacts";
    public const string FrontendPort = "4700";
    public static readonly string FrontendUrl = $"http://127.0.0.1:{FrontendPort}";

    private readonly List<Process> started = [];

    public string Root { get; private init; } = string.Empty;

    public static async Task<Servers> StartAsync(string dbName, bool seedDemoData)
    {
        var servers = new Servers { Root = FindRepoRoot() };
        try
        {
            await servers.RunAsync(dbName, seedDemoData);
        }
        catch
        {
            servers.Dispose();
            throw;
        }

        return servers;
    }

    public void Dispose()
    {
        foreach (var process in started)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Could not stop process {process.Id}: {ex.Message}");
            }
        }
    }

    private async Task RunAsync(string dbName, bool seedDemoData)
    {
        var reuse = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
        var backendEnv = new Dictionary<string, string> { ["ConnectionStrings__Default"] = $"Data Source=database/{dbName}" };
        if (seedDemoData)
        {
            backendEnv["SeedDemoData"] = "true";
        }

        DeleteDatabase(Root, dbName);

        var backendUp = await IsUp(BackendUrl);

        if (!(reuse && backendUp))
        {
            var backend = StartProcess("dotnet", ["run", "--project", Path.Combine("backend", "Api")], Root, backendEnv);
            started.Add(backend);
            await WaitFor(BackendUrl, TimeSpan.FromMinutes(5), backend);
        }

        var frontendUp = await IsUp(FrontendUrl);

        if (!(reuse && frontendUp))
        {
            var frontendDir = Path.Combine(Root, "frontend");
            var frontend = OperatingSystem.IsWindows()
                ? StartProcess("cmd", ["/c", "npm", "run", "start", "--", "--port", FrontendPort], frontendDir)
                : StartProcess("npm", ["run", "start", "--", "--port", FrontendPort], frontendDir);
            started.Add(frontend);
            await WaitFor(FrontendUrl, TimeSpan.FromMinutes(2), frontend);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "global.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root (global.json) not found above " + AppContext.BaseDirectory);
    }

    private static void DeleteDatabase(string root, string dbName)
    {
        var dir = Path.Combine(root, "backend", "Api", "bin", "Debug", "net10.0", "database");
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            var file = Path.Combine(dir, $"{dbName}{suffix}");
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    private static Process StartProcess(string file, string[] args, string workingDir, Dictionary<string, string>? env = null)
    {
        var info = new ProcessStartInfo(file)
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        foreach (var (key, value) in env ?? [])
        {
            info.Environment[key] = value;
        }

        var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {file}");
        // Drain output so the child never blocks on a full pipe.
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static async Task<bool> IsUp(string url)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            using var response = await http.GetAsync(url);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitFor(string url, TimeSpan timeout, Process process)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"Process for {url} exited early with code {process.ExitCode}");
            }

            var up = await IsUp(url);

            if (up)
            {
                return;
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Timed out waiting for {url}");
    }
}
