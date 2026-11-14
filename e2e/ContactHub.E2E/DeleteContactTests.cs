using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ContactHub.E2E;

[TestFixture]
public class DeleteContactTests : E2ETest
{
    private ILocator ConfirmDialog => Page.GetByRole(AriaRole.Alertdialog, new() { Name = "Delete contact" });

    [Test]
    public async Task FromTheList_AfterConfirming_RemovesTheRow()
    {
        var contact = await CreateContactAsync("Ada", Unique("Gone"));
        await SearchListAsync(contact.Surname);

        await Button($"Delete {contact.FullName}").ClickAsync();
        await Assertions.Expect(ConfirmDialog).ToContainTextAsync($"Delete {contact.FullName}?");
        await ConfirmDialog.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Assertions.Expect(Toast("Contact deleted")).ToBeVisibleAsync();
        await Assertions.Expect(Row(contact.Surname)).ToHaveCountAsync(0);
        await Page.GotoAsync($"/contacts/{contact.Id}");
        await Assertions.Expect(Heading("Contact not found")).ToBeVisibleAsync();
    }

    [Test]
    public async Task FromTheList_Cancelling_KeepsTheRow()
    {
        var contact = await CreateContactAsync("Ada", Unique("Kept"));
        await SearchListAsync(contact.Surname);

        await Button($"Delete {contact.FullName}").ClickAsync();
        await ConfirmDialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

        await Assertions.Expect(ConfirmDialog).ToBeHiddenAsync();
        await Assertions.Expect(Row(contact.Surname)).ToBeVisibleAsync();
    }

    [Test]
    public async Task ServerFailure_ShowsAnErrorToast_AndKeepsTheRow()
    {
        var contact = await CreateContactAsync("Ada", Unique("Stuck"));
        await Page.RouteAsync($"**/api/contacts/{contact.Id}", async route =>
        {
            if (route.Request.Method != "DELETE")
            {
                await route.ContinueAsync();
                return;
            }

            await route.FulfillAsync(new()
            {
                Status = 500,
                ContentType = "application/problem+json",
                Body = """{"title":"Delete is broken.","status":500}""",
            });
        });
        await SearchListAsync(contact.Surname);

        await Button($"Delete {contact.FullName}").ClickAsync();
        await ConfirmDialog.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Assertions.Expect(Toast("Could not delete contact")).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator(".p-toast")).ToContainTextAsync("Delete is broken.");
        await Assertions.Expect(Row(contact.Surname)).ToBeVisibleAsync();
    }

    [Test]
    public async Task FromTheDetailPage_ReturnsToTheListWithoutIt()
    {
        var contact = await CreateContactAsync("Ada", Unique("DetailGone"));
        await CreateContactAsync("Ada", Unique("Stays")); // keeps the list out of its empty state
        await Page.GotoAsync($"/contacts/{contact.Id}");

        await Button("Delete").ClickAsync();
        await ConfirmDialog.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"/contacts$"));
        await Assertions.Expect(Toast("Contact deleted")).ToBeVisibleAsync();
        await Page.GetByLabel("Search contacts").FillAsync(contact.Surname);
        await Assertions.Expect(Page.GetByText($"No matches for \"{contact.Surname}\"")).ToBeVisibleAsync();
    }
}
