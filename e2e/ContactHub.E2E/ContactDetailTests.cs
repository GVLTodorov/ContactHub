using Microsoft.Playwright;

namespace ContactHub.E2E;

[TestFixture]
public class ContactDetailTests : E2ETest
{
    [Test]
    public async Task OpensFromTheList_AndShowsEveryField()
    {
        var contact = await CreateContactAsync("Ada", Unique("Detail"), city: "Hamburg", dateOfBirth: "1990-04-12");
        await SearchListAsync(contact.Surname);

        await Page.GetByRole(AriaRole.Link, new() { Name = contact.FullName }).ClickAsync();

        await Assertions.Expect(Heading(contact.FullName)).ToBeVisibleAsync();
        var card = Page.Locator("p-card");
        await Assertions.Expect(card).ToContainTextAsync("Born Apr 12, 1990");
        await Assertions.Expect(card).ToContainTextAsync("Age ");
        await Assertions.Expect(card).ToContainTextAsync("+4930123456");
        await Assertions.Expect(card).ToContainTextAsync("DE89 3704 0044 0532 0130 00");
        await Assertions.Expect(card).ToContainTextAsync("Main St 1");
        await Assertions.Expect(card).ToContainTextAsync("10115 Hamburg, Germany");
        await Assertions.Expect(card).ToContainTextAsync("Created ");
    }

    [TestCase("Copy phone number", "+4930123456")]
    [TestCase("Copy IBAN", "DE89370400440532013000")]
    [TestCase("Copy address", "Main St 1, 10115 Berlin, Germany")]
    public async Task CopyButtons_PutTheValueOnTheClipboard(string button, string expected)
    {
        var contact = await CreateContactAsync("Ada", Unique("Copy"));
        await Page.GotoAsync($"/contacts/{contact.Id}");

        await Button(button).ClickAsync();

        await Assertions.Expect(Page.GetByText("Copied", new() { Exact = true })).ToBeVisibleAsync();
        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

        Assert.That(clipboard, Is.EqualTo(expected));
    }

    [Test]
    public async Task UnknownId_ShowsNotFound_WithAWayBack()
    {
        await Page.GotoAsync($"/contacts/{Guid.NewGuid()}");

        await Assertions.Expect(Heading("Contact not found")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Back to contacts" }).ClickAsync();
        await Assertions.Expect(Heading("Contacts")).ToBeVisibleAsync();
    }

    [Test]
    public async Task LoadError_ShowsTheMessage_AndRetryRecovers()
    {
        var contact = await CreateContactAsync("Ada", Unique("Retry"));
        await Page.RouteAsync($"**/api/contacts/{contact.Id}", route => route.FulfillAsync(new()
        {
            Status = 500,
            ContentType = "application/problem+json",
            Body = """{"title":"The server broke.","status":500}""",
        }));
        await Page.GotoAsync($"/contacts/{contact.Id}");

        await Assertions.Expect(Page.GetByRole(AriaRole.Alert).First).ToContainTextAsync("The server broke.");
        await Page.UnrouteAllAsync();
        await Button("Retry").ClickAsync();

        await Assertions.Expect(Heading(contact.FullName)).ToBeVisibleAsync();
    }
}
