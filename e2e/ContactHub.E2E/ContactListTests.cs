using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ContactHub.E2E;

[TestFixture]
public class ContactListTests : E2ETest
{
    [Test]
    public async Task ShowsEveryColumn_WithTheFormattedIban()
    {
        var contact = await CreateContactAsync("Ada", Unique("Cols"), city: "Hamburg", phone: "+49 40 999999");

        await SearchListAsync(contact.Surname);

        var row = Row(contact.Surname);
        await Assertions.Expect(row.GetByRole(AriaRole.Link, new() { Name = contact.FullName })).ToBeVisibleAsync();
        await Assertions.Expect(row).ToContainTextAsync("Apr 12, 1990");
        await Assertions.Expect(row).ToContainTextAsync("+4940999999");
        await Assertions.Expect(row).ToContainTextAsync("DE89 3704 0044 0532 0130 00");
        await Assertions.Expect(row).ToContainTextAsync("Hamburg");
    }

    [TestCase("name")]
    [TestCase("phone")]
    [TestCase("iban")]
    [TestCase("city")]
    public async Task Search_FindsByEachSupportedField(string field)
    {
        var token = Unique("S");
        var target = await CreateContactAsync(
            field == "name" ? token : "Ada",
            Unique("Find"),
            city: field == "city" ? token : "Berlin",
            iban: field == "iban" ? "FR14 2004 1010 0505 0001 3M02 606" : "DE89 3704 0044 0532 0130 00",
            phone: field == "phone" ? "+33 1 42 68 53 77" : "+49 30 123456");
        var query = field switch
        {
            "phone" => "+33142685377",
            "iban" => "FR14 2004",
            _ => token,
        };

        await SearchListAsync(query);

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = target.FullName })).ToBeVisibleAsync();
        if (field is "name" or "city")
        {
            await Assertions.Expect(NameLinks).ToHaveCountAsync(1);
        }
    }

    [Test]
    public async Task NoMatches_ShowsMessage_AndClearSearchRestoresTheList()
    {
        var contact = await CreateContactAsync("Ada", Unique("Back"));
        var query = Unique("nobody");

        await SearchListAsync(query);

        await Assertions.Expect(Page.GetByText($"No matches for \"{query}\"")).ToBeVisibleAsync();
        await Button("Clear search").ClickAsync();
        await Assertions.Expect(Page.GetByLabel("Search contacts")).ToHaveValueAsync(string.Empty);
        await Page.GetByLabel("Search contacts").FillAsync(contact.Surname);
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = contact.FullName })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ClearButton_EmptiesTheSearchBox()
    {
        await CreateContactAsync("Ada", Unique("Clear"));
        await SearchListAsync("anything");

        await Button("Clear").ClickAsync();

        await Assertions.Expect(Page.GetByLabel("Search contacts")).ToHaveValueAsync(string.Empty);
        await Assertions.Expect(Button("Clear")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Search_IsKeptWhenComingBackFromADetailPage()
    {
        var contact = await CreateContactAsync("Ada", Unique("Keep"));
        await SearchListAsync(contact.Surname);

        await Page.GetByRole(AriaRole.Link, new() { Name = contact.FullName }).ClickAsync();
        await Assertions.Expect(Heading(contact.FullName)).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Back to contacts" }).ClickAsync();

        await Assertions.Expect(Page.GetByLabel("Search contacts")).ToHaveValueAsync(contact.Surname);
        await Assertions.Expect(NameLinks).ToHaveCountAsync(1);
    }

    /// <summary>Three contacts whose values sort differently in every column.</summary>
    private static async Task<string> CreateSortableTrioAsync()
    {
        var token = Unique("Sort");
        await CreateContactAsync(token, "Alpha", city: "Zurich", dateOfBirth: "1990-01-01", phone: "+49 30 111111", iban: "FR14 2004 1010 0505 0001 3M02 606");
        await CreateContactAsync(token, "Beta", city: "Amsterdam", dateOfBirth: "1970-01-01", phone: "+49 30 333333", iban: "GB82 WEST 1234 5698 7654 32");
        await CreateContactAsync(token, "Gamma", city: "Madrid", dateOfBirth: "1980-01-01", phone: "+49 30 222222", iban: "DE89 3704 0044 0532 0130 00");

        return token;
    }

    [Test]
    public async Task SortsBySurname_AndTheNameHeaderReversesIt()
    {
        var token = await CreateSortableTrioAsync();

        await SearchListAsync(token);

        await Assertions.Expect(NameLinks).ToHaveTextAsync([$"{token} Alpha", $"{token} Beta", $"{token} Gamma"]);
        await Page.GetByRole(AriaRole.Columnheader, new() { Name = "Name" }).ClickAsync();
        await Assertions.Expect(NameLinks).ToHaveTextAsync([$"{token} Gamma", $"{token} Beta", $"{token} Alpha"]);
    }

    [TestCase("Date of birth", "Beta", "Gamma", "Alpha")]
    [TestCase("Phone", "Alpha", "Gamma", "Beta")]
    [TestCase("IBAN", "Gamma", "Alpha", "Beta")]
    [TestCase("City", "Beta", "Gamma", "Alpha")]
    public async Task EachColumnHeader_SortsByItsColumn(string header, string first, string second, string third)
    {
        var token = await CreateSortableTrioAsync();
        await SearchListAsync(token);

        await Page.GetByRole(AriaRole.Columnheader, new() { Name = header }).ClickAsync();

        await Assertions.Expect(NameLinks).ToHaveTextAsync([$"{token} {first}", $"{token} {second}", $"{token} {third}"]);
    }

    [Test]
    public async Task Paginator_ShowsTenRowsPerPage_AndCanShowMore()
    {
        var token = Unique("Page");
        for (var i = 0; i < 11; i++)
        {
            await CreateContactAsync(token, $"Row{i:00}");
        }

        await SearchListAsync(token);

        await Assertions.Expect(NameLinks).ToHaveCountAsync(10);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Next Page" }).ClickAsync();
        await Assertions.Expect(NameLinks).ToHaveTextAsync([$"{token} Row10"]);

        await Page.GetByRole(AriaRole.Combobox, new() { Name = "Rows per page" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "25", Exact = true }).ClickAsync();
        await Assertions.Expect(NameLinks).ToHaveCountAsync(11);
    }

    [Test]
    public async Task SlowApi_ShowsTheLoadingSkeleton_ThenTheList()
    {
        await CreateContactAsync("Ada", Unique("Slow"));
        await Page.RouteAsync("**/api/contacts", async route =>
        {
            await Task.Delay(1500);
            await route.ContinueAsync();
        });

        await Page.GotoAsync("/contacts");

        await Assertions.Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Loading contacts" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByLabel("Search contacts")).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Loading contacts" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task EmptyDatabase_ShowsTheFirstContactPrompt()
    {
        await Page.RouteAsync("**/api/contacts", route => route.FulfillAsync(new() { Status = 200, ContentType = "application/json", Body = "[]" }));

        await Page.GotoAsync("/contacts");

        await Assertions.Expect(Page.GetByText("No contacts yet")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create your first contact" }).ClickAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts/new$"));
    }

    [Test]
    public async Task LoadError_ShowsTheMessage_AndRetryRecovers()
    {
        await CreateContactAsync("Ada", Unique("Recover"));
        await Page.RouteAsync("**/api/contacts", route => route.FulfillAsync(new()
        {
            Status = 500,
            ContentType = "application/problem+json",
            Body = """{"title":"The server broke.","status":500}""",
        }));
        await Page.GotoAsync("/contacts");

        await Assertions.Expect(Page.GetByRole(AriaRole.Alert).First).ToContainTextAsync("The server broke.");
        await Page.UnrouteAllAsync();
        await Button("Retry").ClickAsync();

        await Assertions.Expect(Page.GetByLabel("Search contacts")).ToBeVisibleAsync();
    }
}
