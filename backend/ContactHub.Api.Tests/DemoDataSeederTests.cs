using System.Net.Http.Json;
using System.Text.Json;
using ContactHub.Infrastructure.Persistence;

namespace ContactHub.Api.Tests;

[TestFixture]
public class DemoDataSeederTests
{
    [Test]
    public void Seed_EmptyDatabase_InsertsVariedValidContacts()
    {
        using var db = new TestDatabase();
        using var context = db.CreateContext();

        DemoDataSeeder.Seed(context, new FixedTimeProvider());

        var contacts = context.Contacts.ToList();
        Assert.That(contacts, Has.Count.EqualTo(5));
        Assert.That(contacts.Select(c => c.Surname).Distinct().Count(), Is.EqualTo(5));
        Assert.That(contacts.Select(c => c.CreatedAt), Has.All.GreaterThanOrEqualTo(FixedTimeProvider.DefaultNow.UtcDateTime));
    }

    [Test]
    public void Seed_NonEmptyDatabase_DoesNothing()
    {
        using var db = new TestDatabase();
        using var context = db.CreateContext();

        DemoDataSeeder.Seed(context, new FixedTimeProvider());
        DemoDataSeeder.Seed(context, new FixedTimeProvider());

        Assert.That(context.Contacts.Count(), Is.EqualTo(5));
    }
}

[TestFixture]
public class ApiSeedDemoDataTests : ApiFactoryTestBase
{
    protected override Dictionary<string, string?> Settings => new() { ["SeedDemoData"] = "true" };

    [Test]
    public async Task SeedDemoDataFlag_SeedsContactsAtStartup()
    {
        var list = await Client.GetFromJsonAsync<JsonElement>("/api/contacts");

        Assert.That(list.GetArrayLength(), Is.EqualTo(5));
    }
}
