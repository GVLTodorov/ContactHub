using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ContactHub.Api.Tests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace ContactHub.Api.Tests.Api;

public abstract class ApiFactoryTestBase
{
    private WebApplicationFactory<Program> _factory = null!;

    protected HttpClient Client { get; private set; } = null!;

    protected virtual Dictionary<string, string?> Settings => [];

    internal static void ResetTestDatabase()
    {
        SqliteConnection.ClearAllPools();
        var file = Path.Combine(AppContext.BaseDirectory, "database", "contacthub_test.db");
        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }

    [OneTimeSetUp]
    public void CreateFactory()
    {
        ResetTestDatabase();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(Settings));
        });
        Client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void DisposeFactory()
    {
        Client.Dispose();
        _factory.Dispose();
        SqliteConnection.ClearAllPools();
    }

    protected static JsonObject Body(string firstName = "Ada", string surname = "Lovelace") => new()
    {
        ["firstName"] = firstName,
        ["surname"] = surname,
        ["dateOfBirth"] = "1990-04-12",
        ["address"] = new JsonObject
        {
            ["street"] = "Main St 1",
            ["postalCode"] = "10115",
            ["city"] = "Berlin",
            ["country"] = "Germany",
        },
        ["phoneNumber"] = "+49 30 123456",
        ["iban"] = "DE89 3704 0044 0532 0130 00",

        // Used by PUT (a new contact is at version 1); POST ignores it.
        ["version"] = 1,
    };
}

[TestFixture]
public class ApiEndpointTests : ApiFactoryTestBase
{
    [Test]
    public async Task Health_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/health");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(json.GetProperty("status").GetString(), Is.EqualTo("ok"));
    }

    [Test]
    public async Task Contacts_FullLifecycle()
    {
        var create = await Client.PostAsJsonAsync("/api/contacts", Body());
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        Assert.That(create.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(create.Headers.Location!.ToString(), Is.EqualTo($"/api/contacts/{id}"));

        var get = await Client.GetAsync($"/api/contacts/{id}");

        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var list = await Client.GetFromJsonAsync<JsonElement>("/api/contacts");

        Assert.That(list.EnumerateArray().Select(e => e.GetProperty("id").GetGuid()), Does.Contain(id));

        var update = await Client.PutAsJsonAsync($"/api/contacts/{id}", Body("Grace", "Hopper"));
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(update.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(updated.GetProperty("firstName").GetString(), Is.EqualTo("Grace"));

        var delete = await Client.DeleteAsync($"/api/contacts/{id}");
        var afterDelete = await Client.GetAsync($"/api/contacts/{id}");

        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(afterDelete.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_Invalid_ReturnsValidationProblem()
    {
        var body = Body();
        body["iban"] = "nope";
        body["address"]!["city"] = "";

        var response = await Client.PostAsJsonAsync("/api/contacts", body);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var keys = json.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToList();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(keys, Is.EquivalentTo(new[] { "iban", "address.city" }));
    }

    [Test]
    public async Task MissingContact_ReturnsProblemDetails404ForGetPutDelete()
    {
        var id = Guid.NewGuid();

        var get = await Client.GetAsync($"/api/contacts/{id}");
        var put = await Client.PutAsJsonAsync($"/api/contacts/{id}", Body());
        var delete = await Client.DeleteAsync($"/api/contacts/{id}");

        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(put.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task NonGuidId_ReturnsNotFound()
    {
        var response = await Client.GetAsync("/api/contacts/not-a-guid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Swagger_ServesDocumentAndTopBar()
    {
        var doc = await Client.GetAsync("/swagger/v1/swagger.json");
        var ui = await Client.GetAsync("/swagger/index.html");
        var html = await ui.Content.ReadAsStringAsync();

        Assert.That(doc.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(ui.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(html, Does.Contain("topbar-links"));
    }
}

[TestFixture]
public class ApiDefaultConnectionStringTests : ApiFactoryTestBase
{
    protected override Dictionary<string, string?> Settings => new() { ["ConnectionStrings:Default"] = null };

    [Test]
    public async Task MissingConnectionString_FallsBackToDefaultDatabase()
    {
        var response = await Client.GetAsync("/api/contacts");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(File.Exists(Path.Combine(AppContext.BaseDirectory, "database", "contacthub.db")), Is.True);
    }
}
