using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ContactHub.Api.Integration.Tests;

public abstract class IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;

    protected HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public void CreateFactory()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Testing"));
        Client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void DisposeFactory()
    {
        Client.Dispose();
        _factory.Dispose();
    }

    protected static JsonObject ValidBody(string firstName = "Ada", string surname = "Lovelace") => new()
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
    };

    protected async Task<JsonElement> CreateContactAsync(JsonObject? body = null)
    {
        var response = await Client.PostAsJsonAsync("/api/contacts", body ?? ValidBody());
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        return json;
    }

    protected async Task<Guid> CreateContactIdAsync(string firstName = "Ada", string surname = "Lovelace")
    {
        var json = await CreateContactAsync(ValidBody(firstName, surname));

        return json.GetProperty("id").GetGuid();
    }

    protected static async Task<List<string>> ErrorKeysAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var keys = json.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToList();

        return keys;
    }
}
