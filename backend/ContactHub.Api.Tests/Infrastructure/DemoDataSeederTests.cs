using System.Net.Http.Json;
using System.Text.Json;
using ContactHub.Api.Tests.Api;
using ContactHub.Api.Tests.Common;
using ContactHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContactHub.Api.Tests.Infrastructure;

[TestFixture]
public class DemoDataSeederTests
{
    [Test]
    public async Task SeedAsync_EmptyDatabase_InsertsVariedValidContacts()
    {
        using var db = new TestDatabase();
        await using var context = db.CreateContext();

        await DemoDataSeeder.SeedAsync(context, new FixedTimeProvider());

        var contacts = await context.Contacts.ToListAsync();

        Assert.That(contacts, Has.Count.EqualTo(5));
        Assert.That(contacts.Select(c => c.Surname).Distinct().Count(), Is.EqualTo(5));
        Assert.That(contacts.Select(c => c.CreatedAt), Has.All.GreaterThanOrEqualTo(FixedTimeProvider.DefaultNow.UtcDateTime));
    }

    [Test]
    public async Task SeedAsync_NonEmptyDatabase_DoesNothing()
    {
        using var db = new TestDatabase();
        await using var context = db.CreateContext();

        await DemoDataSeeder.SeedAsync(context, new FixedTimeProvider());
        await DemoDataSeeder.SeedAsync(context, new FixedTimeProvider());

        var count = await context.Contacts.CountAsync();

        Assert.That(count, Is.EqualTo(5));
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
