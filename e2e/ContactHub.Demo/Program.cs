using ContactHub.E2E;
using Microsoft.Playwright;

namespace ContactHub.Demo;

internal static class Program
{
    private const string SwaggerUrl = "http://127.0.0.1:8080/swagger";
    private const int StepPauseMs = 1000;
    private const int KeyDelayMs = 60;

    // Playwright videos have no cursor: a dot follows the mouse and pulses on click.
    private const string CursorScript = """
        (() => {
          const KEY = '__demoCursor';
          const setup = () => {
            if (document.getElementById('__demo-cursor')) return;
            const dot = document.createElement('div');
            dot.id = '__demo-cursor';
            dot.style.cssText = 'position:fixed;left:0;top:0;width:18px;height:18px;margin:-9px 0 0 -9px;'
              + 'border-radius:50%;background:rgba(239,68,68,.85);border:2px solid #fff;'
              + 'box-shadow:0 0 6px rgba(0,0,0,.5);pointer-events:none;z-index:2147483647;';
            const saved = sessionStorage.getItem(KEY);
            if (saved) {
              const p = JSON.parse(saved);
              dot.style.left = p.x + 'px';
              dot.style.top = p.y + 'px';
            } else {
              dot.style.display = 'none';
            }
            document.documentElement.appendChild(dot);
            document.addEventListener('mousemove', e => {
              dot.style.display = 'block';
              dot.style.left = e.clientX + 'px';
              dot.style.top = e.clientY + 'px';
              sessionStorage.setItem(KEY, JSON.stringify({ x: e.clientX, y: e.clientY }));
            }, true);
            document.addEventListener('mousedown', () => {
              dot.animate(
                [{ transform: 'scale(1)', opacity: 1 }, { transform: 'scale(2.6)', opacity: .45 }, { transform: 'scale(1)', opacity: 1 }],
                { duration: 380, easing: 'ease-out' });
            }, true);
          };
          if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', setup);
          else setup();
        })();
        """;

    static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "install")
        {
            return Microsoft.Playwright.Program.Main(args);
        }

        try
        {
            using var servers = await Servers.StartAsync("contacthub_demo.db", seedDemoData: true);
            var demoDir = Path.Combine(servers.Root, "e2e", "artifacts", "demo");

            var exitCode = await Record(demoDir);

            return exitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DEMO FAILED: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> Record(string demoDir)
    {
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

        string? failure = null;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { SlowMo = 100 });

        // Warm up the dev server off-camera so the recording never shows a cold first compile.
        var warmup = await browser.NewPageAsync();
        await warmup.GotoAsync(Servers.FrontendUrl);
        await warmup.GetByText("Go to contacts").WaitForAsync(new() { Timeout = 120_000 });
        await warmup.CloseAsync();

        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            RecordVideoDir = rawDir,
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 },
            Permissions = ["clipboard-read", "clipboard-write"],
        });
        await context.AddInitScriptAsync(CursorScript);
        var page = await context.NewPageAsync();

        try
        {
            await Scenario(page);
        }
        catch (Exception ex)
        {
            failure = ex.Message;
        }

        var video = page.Video;
        await context.CloseAsync();
        string? videoPath = null;
        if (video is not null)
        {
            videoPath = await video.PathAsync();
        }

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
        Console.WriteLine($"DEMO RECORDED: {target}");
        return 0;
    }

    private static async Task Scenario(IPage page)
    {
        // 1. Swagger, pause.
        await page.GotoAsync(SwaggerUrl);
        var clientLink = page.Locator(".topbar-links a", new() { HasText = "Client" });
        await clientLink.WaitForAsync();
        await Pause(2000);

        // 2. Move to the green Client button; keep the recording in one tab.
        await clientLink.EvaluateAsync("a => a.target = '_self'");
        await Click(clientLink);
        await page.WaitForURLAsync(url => url.StartsWith(Servers.FrontendUrl, StringComparison.Ordinal));

        // 3. The seeded list (via the home page's "Go to contacts").
        var goToContacts = page.GetByText("Go to contacts");
        await goToContacts.WaitForAsync();
        await Pause(600);
        await Click(goToContacts);
        var firstContact = page.GetByRole(AriaRole.Link, new() { Name = "Ada Lovelace" });
        await firstContact.WaitForAsync();
        await Pause(800);

        // 4. Search by city, then clear.
        await Type(page, page.GetByLabel("Search contacts"), "Berlin");
        await Pause(800);
        await Click(page.GetByRole(AriaRole.Button, new() { Name = "Clear" }));
        await Pause(1000);

        // 5. Open a contact and use the copy buttons.
        await Click(firstContact);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Ada Lovelace" }).WaitForAsync();
        await Pause(1000);
        await Click(page.GetByRole(AriaRole.Button, new() { Name = "Copy phone number" }));
        await Pause(1000);
        await Click(page.GetByRole(AriaRole.Button, new() { Name = "Copy address" }));
        await Pause(800);

        // 6. Back, then a new contact with an invalid IBAN first.
        await Click(page.GetByRole(AriaRole.Link, new() { Name = "Back to contacts" }));
        var newContact = page.GetByRole(AriaRole.Button, new() { Name = "New contact" });
        await newContact.WaitForAsync();
        await Click(newContact);
        await Type(page, page.GetByLabel("First name"), "Nikola");
        await Type(page, page.GetByLabel("Surname"), "Tesla");
        await Type(page, page.GetByLabel("Date of birth"), "1985-07-10");
        await page.Keyboard.PressAsync("Escape");
        await Type(page, page.GetByLabel("Phone number"), "+381 11 123 4567");
        var iban = page.GetByLabel("IBAN");
        await Type(page, iban, "NL91 ABNA 0417 1643 01");
        await page.Keyboard.PressAsync("Tab");
        await Pause(800);
        await iban.ClickAsync();
        await page.Keyboard.PressAsync("Control+A");
        await iban.PressSequentiallyAsync("NL91 ABNA 0417 1643 00", new() { Delay = KeyDelayMs });
        await Pause(800);
        await Type(page, page.GetByLabel("Street"), "Kralja 10");
        await Type(page, page.GetByLabel("Postal code"), "11000");
        await Type(page, page.GetByLabel("City"), "Belgrade");
        await Type(page, page.GetByLabel("Country"), "Serbia");

        // 7. Save, then open the new contact.
        await Click(page.GetByRole(AriaRole.Button, new() { Name = "Save contact" }));
        var created = page.GetByRole(AriaRole.Link, new() { Name = "Nikola Tesla" });
        await created.WaitForAsync();
        await Pause(600);
        await Click(created);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Nikola Tesla" }).WaitForAsync();
        await Pause(600);

        // 8. Edit one field and save.
        await Click(page.GetByRole(AriaRole.Button, new() { Name = "Edit" }));
        var city = page.GetByLabel("City");
        await city.WaitForAsync();
        await Click(city);
        await page.Keyboard.PressAsync("Control+A");
        await city.PressSequentiallyAsync("Smiljan", new() { Delay = KeyDelayMs });
        await Pause(StepPauseMs);
        await Click(page.GetByRole(AriaRole.Button, new() { Name = "Save contact" }));
        await page.GetByText("Smiljan").First.WaitForAsync();
        await Pause(600);

        // 9. Delete with the confirmation dialog; end on the list.
        await Click(page.GetByRole(AriaRole.Button, new() { Name = "Delete" }));
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Delete contact" });
        await dialog.WaitForAsync();
        await Pause(1000);
        await Click(dialog.GetByRole(AriaRole.Button, new() { Name = "Delete" }));
        await page.GetByRole(AriaRole.Link, new() { Name = "Ada Lovelace" }).WaitForAsync();
        await Pause(1000);
    }

    private static async Task Pause(int ms) => await Task.Delay(ms);

    /// <summary>Glide the visible cursor to the element, click it, then wait one step.</summary>
    private static async Task Click(ILocator locator, bool pause = true)
    {
        var page = locator.Page;
        await locator.ScrollIntoViewIfNeededAsync();
        var box = await locator.BoundingBoxAsync() ?? throw new InvalidOperationException("Element has no bounding box");
        var x = box.X + (box.Width / 2);
        var y = box.Y + (box.Height / 2);
        await page.Mouse.MoveAsync(x, y, new() { Steps = 12 });
        await Pause(250);
        await page.Mouse.ClickAsync(x, y);
        if (pause)
        {
            await Pause(StepPauseMs);
        }
    }

    /// <summary>Click a field, type into it key by key, then wait one step.</summary>
    private static async Task Type(IPage page, ILocator field, string text)
    {
        await Click(field, pause: false);
        await field.PressSequentiallyAsync(text, new() { Delay = KeyDelayMs });
        await Pause(StepPauseMs);
    }
}
