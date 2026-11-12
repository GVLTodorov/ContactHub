using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ContactHub.E2E;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "install")
        {
            return Microsoft.Playwright.Program.Main(args);
        }

        try
        {
            using var servers = await Servers.StartAsync("contacthub_e2e.db", seedDemoData: false);
            var artifacts = Path.Combine(servers.Root, "e2e", "artifacts");

            var exitCode = await RunScenario(artifacts);

            return exitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"E2E FAILED: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunScenario(string artifacts)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        await using var context = await browser.NewContextAsync(new() { BaseURL = Servers.FrontendUrl });
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
}
