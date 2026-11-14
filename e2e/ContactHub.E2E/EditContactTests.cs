using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ContactHub.E2E;

[TestFixture]
public class EditContactTests : E2ETest
{
    [Test]
    public async Task Save_UpdatesTheDetailPage_AndShowsAToast()
    {
        var contact = await CreateContactAsync("Ada", Unique("Edit"));
        await Page.GotoAsync($"/contacts/{contact.Id}");
        await Button("Edit").ClickAsync();
        await Assertions.Expect(Page.GetByLabel("First name")).ToHaveValueAsync("Ada");

        await Page.GetByLabel("First name").FillAsync("Grace");
        await Page.GetByLabel("City").FillAsync("Hamburg");
        await Button("Save contact").ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex($"/contacts/{contact.Id}$"));
        await Assertions.Expect(Heading($"Grace {contact.Surname}")).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator("p-card")).ToContainTextAsync("10115 Hamburg, Germany");
        await Assertions.Expect(Toast("Contact updated")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Cancel_ReturnsToTheDetailPage_WithoutChanges()
    {
        var contact = await CreateContactAsync("Ada", Unique("Cancel"));
        await Page.GotoAsync($"/contacts/{contact.Id}/edit");
        await Assertions.Expect(Page.GetByLabel("First name")).ToHaveValueAsync("Ada");

        await Page.GetByLabel("First name").FillAsync("Discarded");
        await Button("Cancel").ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex($"/contacts/{contact.Id}$"));
        await Assertions.Expect(Heading(contact.FullName)).ToBeVisibleAsync();
    }

    [Test]
    public async Task InvalidValue_IsFlagged_AndNothingIsSaved()
    {
        var contact = await CreateContactAsync("Ada", Unique("Bad"));
        await Page.GotoAsync($"/contacts/{contact.Id}/edit");
        await Assertions.Expect(Page.GetByLabel("First name")).ToHaveValueAsync("Ada");

        await Page.GetByLabel("IBAN").FillAsync("DE00 3704 0044 0532 0130 00");
        await Button("Save contact").ClickAsync();

        await Assertions.Expect(Page.GetByText("Enter a valid IBAN.")).ToBeVisibleAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex($"/contacts/{contact.Id}/edit$"));
    }

    [Test]
    public async Task SomeoneElseSavedFirst_ShowsTheConflict_AndKeepsTheirChange()
    {
        var contact = await CreateContactAsync("Ada", Unique("Race"));
        await Page.GotoAsync($"/contacts/{contact.Id}/edit");
        await Assertions.Expect(Page.GetByLabel("First name")).ToHaveValueAsync("Ada");

        await RenameContactViaApiAsync(contact, "Other");
        await Page.GetByLabel("First name").FillAsync("Mine");
        await Button("Save contact").ClickAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Alert).First).ToContainTextAsync("changed by someone else");
        await Page.GotoAsync($"/contacts/{contact.Id}");
        await Assertions.Expect(Heading($"Other {contact.Surname}")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TypedEdits_SurviveTheBackgroundRefresh()
    {
        var contact = await CreateContactAsync("Ada", Unique("Slow"));
        await Page.GotoAsync($"/contacts/{contact.Id}");
        await Assertions.Expect(Heading(contact.FullName)).ToBeVisibleAsync();

        // A slow API (like a cold Render instance): the edit page shows the cached contact at once,
        // and its re-fetch lands while the user is typing.
        await Page.RouteAsync($"**/api/contacts/{contact.Id}", async route =>
        {
            await Task.Delay(2000);
            await route.ContinueAsync();
        });
        var refresh = Page.WaitForResponseAsync(r => r.Url.EndsWith($"/api/contacts/{contact.Id}", StringComparison.Ordinal));
        await Button("Edit").ClickAsync();
        await Page.GetByLabel("First name").FillAsync("Augusta");
        var refreshed = await refresh;

        Assert.That(refreshed.Ok, Is.True);

        await Assertions.Expect(Page.GetByLabel("First name")).ToHaveValueAsync("Augusta");
        await Button("Save contact").ClickAsync();
        await Assertions.Expect(Heading($"Augusta {contact.Surname}")).ToBeVisibleAsync();
    }

    [Test]
    public async Task LoadError_ShowsTheMessage()
    {
        var contact = await CreateContactAsync("Ada", Unique("EditFail"));
        await Page.RouteAsync($"**/api/contacts/{contact.Id}", route => route.FulfillAsync(new()
        {
            Status = 500,
            ContentType = "application/problem+json",
            Body = """{"title":"The server broke.","status":500}""",
        }));

        await Page.GotoAsync($"/contacts/{contact.Id}/edit");

        await Assertions.Expect(Page.GetByRole(AriaRole.Alert).First).ToContainTextAsync("The server broke.");
        await Assertions.Expect(Page.GetByLabel("First name")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task UnknownId_ShowsNotFound()
    {
        await Page.GotoAsync($"/contacts/{Guid.NewGuid()}/edit");

        await Assertions.Expect(Page.GetByText("This contact does not exist or was removed.")).ToBeVisibleAsync();
        await Button("Back to list").ClickAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts$"));
    }
}
