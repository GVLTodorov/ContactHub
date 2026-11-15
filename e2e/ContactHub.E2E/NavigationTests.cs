using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ContactHub.E2E;

[TestFixture]
public class NavigationTests : E2ETest
{
    [Test]
    public async Task HomePage_LeadsToTheContacts()
    {
        await Page.GotoAsync("/");

        await Assertions.Expect(Heading("Welcome to ContactHub")).ToBeVisibleAsync();
        await Button("Go to contacts").ClickAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts$"));
    }

    [Test]
    public async Task Sidebar_MarksTheCurrentPage_AndTheLogoGoesHome()
    {
        var nav = Page.GetByRole(AriaRole.Navigation, new() { Name = "Main" });
        await Page.GotoAsync("/");

        await nav.GetByRole(AriaRole.Link, new() { Name = "Contacts" }).ClickAsync();
        await Assertions.Expect(Heading("Contacts")).ToBeVisibleAsync();
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Contacts" })).ToHaveAttributeAsync("aria-current", "page");

        await Page.GetByRole(AriaRole.Link, new() { Name = "ContactHub" }).ClickAsync();
        await Assertions.Expect(Heading("Welcome to ContactHub")).ToBeVisibleAsync();
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Home" })).ToHaveAttributeAsync("aria-current", "page");
    }

    [Test]
    public async Task NewContactButton_OpensTheForm()
    {
        await Page.GotoAsync("/contacts");

        await Button("New contact").ClickAsync();

        await Assertions.Expect(Heading("New contact")).ToBeVisibleAsync();
    }

    [Test]
    public async Task UnknownRoute_ShowsPageNotFound_WithALinkHome()
    {
        await Page.GotoAsync("/no/such/page");

        await Assertions.Expect(Heading("Page not found")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Back to home" }).ClickAsync();
        await Assertions.Expect(Heading("Welcome to ContactHub")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ApiStatus_IsShownInTheFooter()
    {
        await Page.GotoAsync("/");

        await Assertions.Expect(Page.GetByText("API online")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ApiStatus_ShowsChecking_WhileTheHealthCheckRuns()
    {
        await Page.RouteAsync("**/api/health", async route =>
        {
            await Task.Delay(1500);
            await route.ContinueAsync();
        });

        await Page.GotoAsync("/");

        await Assertions.Expect(Page.GetByText("API checking…")).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("API online")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ApiStatus_ShowsOffline_WhenTheHealthCheckFails()
    {
        await Page.RouteAsync("**/api/health", route => route.AbortAsync());

        await Page.GotoAsync("/");

        await Assertions.Expect(Page.GetByText("API offline")).ToBeVisibleAsync();
    }

    [Test]
    public async Task SwaggerClientButton_OpensTheApp()
    {
        await Page.GotoAsync("http://127.0.0.1:8080/swagger");
        var client = Page.Locator(".topbar-links a", new() { HasText = "Client" });

        await Assertions.Expect(client).ToHaveAttributeAsync("href", Servers.FrontendUrl);
        await client.EvaluateAsync("a => a.target = '_self'");
        await client.ClickAsync();

        await Assertions.Expect(Heading("Welcome to ContactHub")).ToBeVisibleAsync();
    }
}

[TestFixture]
public class MobileLayoutTests : E2ETest
{
    protected override ViewportSize Viewport => new() { Width = 390, Height = 844 };

    [Test]
    public async Task NarrowScreen_StacksEachContactWithInlineLabels()
    {
        var contact = await CreateContactAsync("Ada", Unique("Phone"));

        await SearchListAsync(contact.Surname);

        await Assertions.Expect(Page.GetByRole(AriaRole.Columnheader, new() { Name = "Date of birth" })).ToBeHiddenAsync();
        var row = Row(contact.Surname);
        await Assertions.Expect(row.GetByText("Date of birth", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(row.GetByText("IBAN", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(row.GetByRole(AriaRole.Link, new() { Name = contact.FullName })).ToBeVisibleAsync();
    }
}
