using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ContactHub.E2E;

[TestFixture]
public class CreateContactTests : E2ETest
{
    [Test]
    public async Task CreatesContact_AndShowsItInTheList()
    {
        var surname = Unique("Tester");

        await Page.GotoAsync("/contacts/new");
        await FillValidFormAsync("Ada", surname);
        await Button("Save contact").ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts$"));
        await Assertions.Expect(Toast("Contact created")).ToBeVisibleAsync();
        await Page.GetByLabel("Search contacts").FillAsync(surname);
        var row = Row(surname);
        await Assertions.Expect(row.GetByRole(AriaRole.Link, new() { Name = $"Ada {surname}" })).ToBeVisibleAsync();
        await Assertions.Expect(row.GetByRole(AriaRole.Cell, new() { Name = "GB82 WEST 1234 5698 7654 32" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EmptySubmit_ShowsRequiredErrors_AndStaysOnTheForm()
    {
        await Page.GotoAsync("/contacts/new");

        await Button("Save contact").ClickAsync();

        // First name, surname, date of birth, phone, IBAN and the four address fields.
        await Assertions.Expect(Page.GetByText("This field is required.")).ToHaveCountAsync(9);
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts/new$"));
    }

    [Test]
    public async Task InvalidIbanAndPhone_AreFlagged_ThenSaveWorksOnceFixed()
    {
        var surname = Unique("Fixer");
        await Page.GotoAsync("/contacts/new");
        await FillValidFormAsync("Ada", surname);

        await Page.GetByLabel("IBAN").FillAsync("NL91 ABNA 0417 1643 01");
        await Page.GetByLabel("Phone number").FillAsync("12");
        await Button("Save contact").ClickAsync();

        await Assertions.Expect(Page.GetByText("Enter a valid IBAN.")).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("Use 7 to 15 digits, optionally starting with +.")).ToBeVisibleAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts/new$"));

        await Page.GetByLabel("IBAN").FillAsync("NL91 ABNA 0417 1643 00");
        await Page.GetByLabel("Phone number").FillAsync("+31 20 123 4567");
        await Button("Save contact").ClickAsync();

        // Search: with more than 10 contacts the new row can be on another page.
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts$"));
        await Page.GetByLabel("Search contacts").FillAsync(surname);
        await Assertions.Expect(Row(surname)).ToBeVisibleAsync();
    }

    [TestCase("2999-01-01", "This field is required.")] // the picker refuses dates after today
    [TestCase("1800-01-01", "Date of birth cannot be over 150 years ago.")]
    public async Task DateOfBirthOutOfRange_IsRejected(string typed, string message)
    {
        await Page.GotoAsync("/contacts/new");
        await FillValidFormAsync("Ada", Unique("Dob"));

        await Page.GetByLabel("Date of birth").FillAsync(string.Empty);
        await FillDateOfBirthAsync(typed);
        await Button("Save contact").ClickAsync();

        var dobField = Page.Locator("div:has(> label[for='dateOfBirth'])");
        await Assertions.Expect(dobField.GetByText(message)).ToBeVisibleAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts/new$"));
    }

    [Test]
    public async Task ServerValidationErrors_AreShownUnderTheMatchingField()
    {
        // The browser rules mirror the server's, so a server-only error is simulated at the network layer.
        await Page.RouteAsync("**/api/contacts", async route =>
        {
            if (route.Request.Method != "POST")
            {
                await route.ContinueAsync();
                return;
            }

            await route.FulfillAsync(new()
            {
                Status = 400,
                ContentType = "application/problem+json",
                Body = """{"title":"One or more validation errors occurred.","status":400,"errors":{"address.city":["City is not known."]}}""",
            });
        });
        await Page.GotoAsync("/contacts/new");
        await FillValidFormAsync("Ada", Unique("Server"));

        await Button("Save contact").ClickAsync();

        var cityField = Page.Locator("div", new() { Has = Page.GetByLabel("City") }).Last;
        await Assertions.Expect(cityField.GetByText("City is not known.")).ToBeVisibleAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts/new$"));
    }

    [Test]
    public async Task Cancel_ReturnsToTheList()
    {
        await Page.GotoAsync("/contacts/new");
        await Page.GetByLabel("First name").FillAsync("Never saved");

        await Button("Cancel").ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts$"));
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Never saved" })).ToHaveCountAsync(0);
    }
}
