using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework.Interfaces;

namespace ContactHub.E2E;

/// <summary>
/// Base for every e2e test: one Chromium per fixture, a fresh browser context per test (own storage,
/// clipboard permission), a screenshot and trace on failure, and helpers that create test data
/// through the API so each test sets up only what it needs. Tests share one database, so each one
/// uses a unique token in its names and searches for it instead of counting all rows.
/// </summary>
public abstract class E2ETest
{
    private const string ApiBase = "http://127.0.0.1:8080";

    private static readonly HttpClient Api = new() { BaseAddress = new Uri(ApiBase) };

    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    private ICDPSession? _cdp;
    private static readonly HttpClient Assets = new();

    /// <summary>
    /// Takes the precise V8 coverage of the page and writes the scripts served by the dev server to
    /// e2e/artifacts/coverage/frontend/raw, each with its source and source map for the converter.
    /// </summary>
    private async Task SaveFrontendCoverage()
    {
        var cdp = _cdp;
        _cdp = null;
        if (cdp is null)
        {
            return;
        }

        try
        {
            var taken = await cdp.SendAsync("Profiler.takePreciseCoverage");
            await cdp.SendAsync("Profiler.stopPreciseCoverage");

            var entries = new List<Dictionary<string, object?>>();
            foreach (var script in taken!.Value.GetProperty("result").EnumerateArray())
            {
                var url = script.GetProperty("url").GetString() ?? string.Empty;
                if (!url.StartsWith(Servers.FrontendUrl, StringComparison.Ordinal))
                {
                    continue;
                }

                var source = await Assets.GetStringAsync(url);
                string? sourceMap = null;
                var match = Regex.Match(source, @"//# sourceMappingURL=(\S+)\s*$");
                if (match.Success)
                {
                    var reference = match.Groups[1].Value;
                    if (reference.StartsWith("data:", StringComparison.Ordinal))
                    {
                        // Inline map: data:application/json;base64,<payload>
                        var payload = reference[(reference.IndexOf(',') + 1)..];
                        sourceMap = reference.Contains(";base64,", StringComparison.Ordinal)
                            ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload))
                            : Uri.UnescapeDataString(payload);
                    }
                    else
                    {
                        sourceMap = await Assets.GetStringAsync(new Uri(new Uri(url), reference));
                    }
                }

                entries.Add(new()
                {
                    ["url"] = url,
                    ["scriptId"] = script.GetProperty("scriptId").GetString(),
                    ["source"] = source,
                    ["functions"] = script.GetProperty("functions"),
                    ["sourceMap"] = sourceMap is null ? null : JsonSerializer.Deserialize<JsonElement>(sourceMap),
                });
            }

            var rawDir = Path.Combine(E2ESetup.Servers.CoverageDir, "frontend", "raw");
            Directory.CreateDirectory(rawDir);
            var file = Path.Combine(rawDir, $"{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(file, JsonSerializer.Serialize(entries));
        }
        catch (Exception ex) when (ex is PlaywrightException or HttpRequestException or IOException)
        {
            TestContext.Progress.WriteLine($"Could not save frontend coverage: {ex.Message}");
        }
    }

    protected IPage Page { get; private set; } = null!;

    /// <summary>Viewport for the test's browser context; override for mobile layouts.</summary>
    protected virtual ViewportSize Viewport => new() { Width = 1280, Height = 800 };

    [OneTimeSetUp]
    public async Task LaunchBrowser()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
    }

    [OneTimeTearDown]
    public async Task CloseBrowser()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    [SetUp]
    public async Task OpenPage()
    {
        _context = await _browser.NewContextAsync(new()
        {
            BaseURL = Servers.FrontendUrl,
            ViewportSize = Viewport,
            Permissions = ["clipboard-read", "clipboard-write"],
        });
        await _context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = false });
        Page = await _context.NewPageAsync();

        if (Servers.Coverage)
        {
            _cdp = await _context.NewCDPSessionAsync(Page);
            await _cdp.SendAsync("Profiler.enable");
            await _cdp.SendAsync("Profiler.startPreciseCoverage", new Dictionary<string, object> { ["callCount"] = true, ["detailed"] = true });
        }
    }

    [TearDown]
    public async Task ClosePage()
    {
        try
        {
            await SaveFrontendCoverage();

            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                await SaveFailureArtifacts();
            }
            else
            {
                await _context.Tracing.StopAsync();
            }
        }
        finally
        {
            await _context.DisposeAsync();
        }
    }

    /// <summary>A short token unique to this run, used to keep a test's rows apart from the others'.</summary>
    protected static string Unique(string prefix) => $"{prefix}{Guid.NewGuid().ToString("N")[..8]}";

    protected static async Task<CreatedContact> CreateContactAsync(
        string firstName,
        string surname,
        string city = "Berlin",
        string iban = "DE89 3704 0044 0532 0130 00",
        string phone = "+49 30 123456",
        string dateOfBirth = "1990-04-12")
    {
        var body = new
        {
            firstName,
            surname,
            dateOfBirth,
            address = new { street = "Main St 1", postalCode = "10115", city, country = "Germany" },
            phoneNumber = phone,
            iban,
        };
        using var response = await Api.PostAsJsonAsync("/api/contacts", body);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new CreatedContact(json.GetProperty("id").GetGuid(), json.GetProperty("version").GetInt32(), firstName, surname);
    }

    /// <summary>Changes a contact behind the browser's back, as another user would.</summary>
    protected static async Task RenameContactViaApiAsync(CreatedContact contact, string newFirstName)
    {
        var body = new
        {
            version = contact.Version,
            firstName = newFirstName,
            surname = contact.Surname,
            dateOfBirth = "1990-04-12",
            address = new { street = "Main St 1", postalCode = "10115", city = "Berlin", country = "Germany" },
            phoneNumber = "+49 30 123456",
            iban = "DE89 3704 0044 0532 0130 00",
        };
        using var response = await Api.PutAsJsonAsync($"/api/contacts/{contact.Id}", body);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Opens the list and narrows it to rows containing <paramref name="query"/>.</summary>
    protected async Task SearchListAsync(string query)
    {
        await Page.GotoAsync("/contacts");
        await Page.GetByLabel("Search contacts").FillAsync(query);
    }

    protected ILocator Row(string text) => Page.GetByRole(AriaRole.Row).Filter(new() { HasText = text });

    /// <summary>Contact name links in the list, in display order.</summary>
    protected ILocator NameLinks => Page.Locator("tbody a");

    protected ILocator Toast(string summary) => Page.Locator(".p-toast").GetByText(summary);

    protected async Task FillDateOfBirthAsync(string isoDate)
    {
        await Page.GetByLabel("Date of birth").PressSequentiallyAsync(isoDate);
        await Page.Keyboard.PressAsync("Escape");
    }

    protected async Task FillValidFormAsync(string firstName, string surname)
    {
        await Page.GetByLabel("First name").FillAsync(firstName);
        await Page.GetByLabel("Surname").FillAsync(surname);
        await FillDateOfBirthAsync("1990-05-17");
        await Page.GetByLabel("Phone number").FillAsync("+359888123456");
        await Page.GetByLabel("IBAN").FillAsync("GB82WEST12345698765432");
        await Page.GetByLabel("Street").FillAsync("1 Main Street");
        await Page.GetByLabel("Postal code").FillAsync("1000");
        await Page.GetByLabel("City").FillAsync("Sofia");
        await Page.GetByLabel("Country").FillAsync("Bulgaria");
    }

    /// <summary>
    /// A button by its exact label. PrimeIcons draw their glyph with CSS <c>::before</c>, and Chromium
    /// counts that private-use character in the accessible name, so leading glyphs are allowed.
    /// </summary>
    protected ILocator Button(string name) =>
        Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex($"^[\\uE000-\\uF8FF\\s]*{Regex.Escape(name)}$") });

    protected ILocator Heading(string name) => Page.GetByRole(AriaRole.Heading, new() { Name = name });

    /// <summary>Writes a screenshot and a Playwright trace to e2e/artifacts and attaches them to the result.</summary>
    private async Task SaveFailureArtifacts()
    {
        var artifacts = Path.Combine(E2ESetup.Servers.Root, "e2e", "artifacts");
        Directory.CreateDirectory(artifacts);
        var name = TestContext.CurrentContext.Test.Name;
        var screenshot = Path.Combine(artifacts, $"{name}.png");
        var trace = Path.Combine(artifacts, $"{name}.trace.zip");

        try
        {
            await Page.ScreenshotAsync(new() { Path = screenshot, FullPage = true });
            TestContext.AddTestAttachment(screenshot, "Page at failure");
        }
        catch (PlaywrightException ex)
        {
            TestContext.Progress.WriteLine($"Could not take a screenshot: {ex.Message}");
        }

        await _context.Tracing.StopAsync(new() { Path = trace });
        TestContext.AddTestAttachment(trace, "Playwright trace (npx playwright show-trace)");
    }
}

public sealed record CreatedContact(Guid Id, int Version, string FirstName, string Surname)
{
    public string FullName => $"{FirstName} {Surname}";
}
