using System.Diagnostics;
using System.Runtime.InteropServices;

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

    // On Windows every started server joins this job: the OS kills the whole tree (cmd -> npm ->
    // node) when the job closes, also when the test run is stopped or crashes before Dispose runs.
    private readonly KillOnCloseJob? job = OperatingSystem.IsWindows() ? KillOnCloseJob.TryCreate() : null;

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
        job?.Dispose();

        foreach (var process in started)
        {
            try
            {
                if (process.WaitForExit(5_000))
                {
                    continue;
                }

                if (OperatingSystem.IsWindows())
                {
                    // Fallback when there is no job: cmd -> npm -> cmd -> node (ng serve).
                    // Process.Kill(true) can miss the grandchildren once their parent dies first.
                    using var taskkill = Process.Start(new ProcessStartInfo("taskkill", ["/PID", process.Id.ToString(), "/T", "/F"])
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    });
                    taskkill?.WaitForExit(10_000);
                }

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

        if (reuse && backendUp)
        {
            Console.Error.WriteLine($"E2E: reusing the backend already running at {BackendUrl} (its own build and database). Stop it to test the current code.");
        }
        else
        {
            var backend = StartProcess("dotnet", ["run", "--project", Path.Combine("backend", "ContactHub.Api")], Root, backendEnv);
            started.Add(backend);
            await WaitFor(BackendUrl, TimeSpan.FromMinutes(5), backend);
        }

        var frontendUp = await IsUp(FrontendUrl);

        if (reuse && frontendUp)
        {
            Console.Error.WriteLine($"E2E: reusing the frontend already running at {FrontendUrl}.");
        }
        else
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
        var dir = Path.Combine(root, "backend", "ContactHub.Api", "bin", "Debug", "net10.0", "database");
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            var file = Path.Combine(dir, $"{dbName}{suffix}");
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    private Process StartProcess(string file, string[] args, string workingDir, Dictionary<string, string>? env = null)
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
        // Joined right after start, before cmd/dotnet get to spawn their own children.
        job?.Add(process);
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

    /// <summary>A Windows job object that kills every process in it when its handle closes.</summary>
    private sealed class KillOnCloseJob : IDisposable
    {
        private const int JobObjectExtendedLimitInformation = 9;
        private const uint JobObjectLimitKillOnJobClose = 0x2000;

        private nint handle;

        private KillOnCloseJob(nint handle) => this.handle = handle;

        public static KillOnCloseJob? TryCreate()
        {
            var handle = CreateJobObjectW(0, null);
            if (handle == 0)
            {
                return null;
            }

            var limits = new ExtendedLimitInformation { BasicLimitInformation = { LimitFlags = JobObjectLimitKillOnJobClose } };
            if (!SetInformationJobObject(handle, JobObjectExtendedLimitInformation, ref limits, (uint)Marshal.SizeOf<ExtendedLimitInformation>()))
            {
                CloseHandle(handle);
                return null;
            }

            return new KillOnCloseJob(handle);
        }

        public void Add(Process process)
        {
            if (!AssignProcessToJobObject(handle, process.Handle))
            {
                Console.Error.WriteLine($"Could not add process {process.Id} to the cleanup job (error {Marshal.GetLastPInvokeError()}).");
            }
        }

        public void Dispose()
        {
            if (handle != 0)
            {
                CloseHandle(handle);
                handle = 0;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern nint CreateJobObjectW(nint attributes, string? name);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetInformationJobObject(nint job, int infoClass, ref ExtendedLimitInformation info, uint length);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(nint job, nint process);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(nint handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct BasicLimitInformation
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public nuint MinimumWorkingSetSize;
            public nuint MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public nuint Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ExtendedLimitInformation
        {
            public BasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public nuint ProcessMemoryLimit;
            public nuint JobMemoryLimit;
            public nuint PeakProcessMemoryUsed;
            public nuint PeakJobMemoryUsed;
        }
    }
}
