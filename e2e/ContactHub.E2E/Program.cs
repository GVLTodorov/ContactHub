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

        var demo = args.Length > 0 && args[0] == "demo";
        var started = new List<Process>();
        try
        {
            var root = FindRepoRoot();
            var artifacts = Path.Combine(root, "e2e", "artifacts");
            var reuse = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
            var dbName = demo ? "contacthub_demo.db" : "contacthub_e2e.db";
            var backendEnv = new Dictionary<string, string> { ["ConnectionStrings__Default"] = $"Data Source=database/{dbName}" };
            if (demo)
            {
                backendEnv["SeedDemoData"] = "true";
            }

            DeleteDatabase(root, dbName);

            var backendUp = await IsUp(BackendUrl);

            if (!(reuse && backendUp))
            {
                var backend = StartProcess("dotnet", ["run", "--project", Path.Combine("backend", "Api")], root, backendEnv);
                started.Add(backend);
                await WaitFor(BackendUrl, TimeSpan.FromMinutes(5), backend);
            }

            var frontendUp = await IsUp(FrontendUrl);

            if (!(reuse && frontendUp))
            {
                var frontend = OperatingSystem.IsWindows()
                    ? StartProcess("cmd", ["/c", "npm", "run", "start", "--", "--port", FrontendPort], Path.Combine(root, "frontend"))
                    : StartProcess("npm", ["run", "start", "--", "--port", FrontendPort], Path.Combine(root, "frontend"));
                started.Add(frontend);
                await WaitFor(FrontendUrl, TimeSpan.FromMinutes(2), frontend);
            }

            var exitCode = demo ? await RunDemo(artifacts) : await RunScenario(artifacts);

            return exitCode;
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

    private static async Task<int> RunDemo(string artifacts)
    {
        var demoDir = Path.Combine(artifacts, "demo");
        var rawDir = Path.Combine(demoDir, "raw");
        if (Directory.Exists(rawDir))
        {
            Directory.Delete(rawDir, recursive: true);
        }

        Directory.CreateDirectory(rawDir);
        var target = Path.Combine(demoDir, "demo.webm");
        if (File.Exists(target))
        {
            File.Delete(target);
        }

        var size = new RecordVideoSize { Width = 1280, Height = 720 };
        string? videoPath = null;
        var failure = (string?)null;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { SlowMo = 300 });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            RecordVideoDir = rawDir,
            RecordVideoSize = size,
        });
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync("http://127.0.0.1:8080/swagger");
            await page.Locator(".topbar-links a", new() { HasText = "Client" }).WaitForAsync();
            await page.WaitForTimeoutAsync(1500);

            var clientLink = page.GetByRole(AriaRole.Link, new() { Name = "Client", Exact = true });
            // The link opens a new tab; keep the demo in one recorded page.
            await clientLink.EvaluateAsync("a => a.target = '_self'");
            await clientLink.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"127\.0\.0\.1:4700"));

            await page.GotoAsync($"{FrontendUrl}/contacts");
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Ada Lovelace" })).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(1500);

            await page.GetByRole(AriaRole.Button, new() { Name = "New contact" }).ClickAsync();
            await page.GetByLabel("First name").FillAsync("Nikola");
            await page.GetByLabel("Surname").FillAsync("Tesla");
            await page.GetByLabel("Date of birth").PressSequentiallyAsync("1985-07-10");
            await page.Keyboard.PressAsync("Escape");
            await page.GetByLabel("Phone number").FillAsync("+381 11 123 4567");
            await page.GetByLabel("IBAN").FillAsync("NL91 ABNA 0417 1643 00");
            await page.GetByLabel("Street").FillAsync("Bulevar Kralja Aleksandra 10");
            await page.GetByLabel("Postal code").FillAsync("11000");
            await page.GetByLabel("City").FillAsync("Belgrade");
            await page.GetByLabel("Country").FillAsync("Serbia");
            await page.GetByRole(AriaRole.Button, new() { Name = "Save contact" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/contacts$"));
            await page.WaitForTimeoutAsync(1000);

            await page.GetByRole(AriaRole.Link, new() { Name = "Nikola Tesla" }).ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Nikola Tesla" })).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(1500);

            await page.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
            await page.GetByLabel("City").FillAsync("Smiljan");
            await page.GetByRole(AriaRole.Button, new() { Name = "Save contact" }).ClickAsync();
            await Assertions.Expect(page.GetByText("Smiljan")).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(1500);

            await page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
            await page.WaitForTimeoutAsync(1000);
            await page.GetByRole(AriaRole.Alertdialog).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/contacts$"));
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Nikola Tesla" })).ToHaveCountAsync(0);
            await page.WaitForTimeoutAsync(2000);
        }
        catch (Exception ex)
        {
            failure = ex.Message;
        }

        videoPath = page.Video is null ? null : await page.Video.PathAsync();
        await context.CloseAsync();

        if (failure is not null)
        {
            Console.Error.WriteLine($"DEMO FAILED: {failure}");
            return 1;
        }

        if (videoPath is null || !File.Exists(videoPath))
        {
            Console.Error.WriteLine("DEMO FAILED: no video was recorded");
            return 1;
        }

        File.Move(videoPath, target, overwrite: true);
        Directory.Delete(rawDir, recursive: true);
        Console.WriteLine($"DEMO PASSED: {target}");
        return 0;
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
