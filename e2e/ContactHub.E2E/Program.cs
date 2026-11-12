using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ContactHub.E2E;

internal static class Program
{
    private const string BackendUrl = "http://127.0.0.1:8080/api/contacts";
    private const string FrontendPort = "4700";
    private static readonly string FrontendUrl = $"http://127.0.0.1:{FrontendPort}";

    static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "install")
        {
            return Microsoft.Playwright.Program.Main(args);
        }

        var started = new List<Process>();
        try
        {
            var root = FindRepoRoot();
            var artifacts = Path.Combine(root, "e2e", "artifacts");
            var reuse = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));

            DeleteE2EDatabase(root);

            if (!(reuse && await IsUp(BackendUrl)))
            {
                var backend = StartProcess(
                    "dotnet",
                    ["run", "--project", Path.Combine("backend", "src", "Api")],
                    root,
                    new Dictionary<string, string> { ["ConnectionStrings__Default"] = "Data Source=database/contacthub_e2e.db" });
                started.Add(backend);
                await WaitFor(BackendUrl, TimeSpan.FromMinutes(5), backend);
            }

            if (!(reuse && await IsUp(FrontendUrl)))
            {
                var frontend = OperatingSystem.IsWindows()
                    ? StartProcess("cmd", ["/c", "npm", "run", "start", "--", "--port", FrontendPort], Path.Combine(root, "frontend"))
                    : StartProcess("npm", ["run", "start", "--", "--port", FrontendPort], Path.Combine(root, "frontend"));
                started.Add(frontend);
                await WaitFor(FrontendUrl, TimeSpan.FromMinutes(2), frontend);
            }

            return await RunScenario(artifacts);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"E2E FAILED: {ex.Message}");
            return 1;
        }
        finally
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
    }

    private static async Task<int> RunScenario(string artifacts)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        await using var context = await browser.NewContextAsync(new() { BaseURL = FrontendUrl });
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = false });
        var page = await context.NewPageAsync();

        try
        {
            var surname = $"Tester{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            await page.GotoAsync("/contacts/new");
            await page.GetByLabel("First name").FillAsync("Ada");
            await page.GetByLabel("Surname").FillAsync(surname);
            await page.GetByLabel("Date of birth").PressSequentiallyAsync("1990-05-17");
            await page.Keyboard.PressAsync("Escape");
            await page.GetByLabel("Phone number").FillAsync("+359888123456");
            await page.GetByLabel("IBAN").FillAsync("GB82WEST12345698765432");
            await page.GetByLabel("Street").FillAsync("1 Main Street");
            await page.GetByLabel("Postal code").FillAsync("1000");
            await page.GetByLabel("City").FillAsync("Sofia");
            await page.GetByLabel("Country").FillAsync("Bulgaria");
            await page.GetByRole(AriaRole.Button, new() { Name = "Save contact" }).ClickAsync();

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/contacts$"));
            var row = page.GetByRole(AriaRole.Row).Filter(new() { HasText = surname });
            await Assertions.Expect(row.GetByRole(AriaRole.Link, new() { Name = $"Ada {surname}" })).ToBeVisibleAsync();
            await Assertions.Expect(row.GetByRole(AriaRole.Cell, new() { Name = "GB82 WEST 1234 5698 7654 32" })).ToBeVisibleAsync();

            await context.Tracing.StopAsync();
            Console.WriteLine("E2E PASSED: creates a contact and shows it in the list");
            return 0;
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory(artifacts);
            var screenshot = Path.Combine(artifacts, "failure.png");
            var trace = Path.Combine(artifacts, "trace.zip");
            try
            {
                await page.ScreenshotAsync(new() { Path = screenshot, FullPage = true });
            }
            catch
            {
                // best effort
            }

            await context.Tracing.StopAsync(new() { Path = trace });
            Console.Error.WriteLine($"E2E FAILED: {ex.Message}");
            Console.Error.WriteLine($"Screenshot: {screenshot}");
            Console.Error.WriteLine($"Trace: {trace}");
            return 1;
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

    private static void DeleteE2EDatabase(string root)
    {
        var dir = Path.Combine(root, "backend", "src", "Api", "bin", "Debug", "net10.0", "database");
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            var file = Path.Combine(dir, $"contacthub_e2e.db{suffix}");
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

            if (await IsUp(url))
            {
                return;
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Timed out waiting for {url}");
    }
}
