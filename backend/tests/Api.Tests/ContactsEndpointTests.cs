using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

[TestFixture]
public class ContactsEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Testing"));
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static object Body(string firstName, string surname) => new
    {
        firstName,
        surname,
        dateOfBirth = "1990-04-12",
        address = new { street = "Main St 1", postalCode = "10115", city = "Berlin", country = "Germany" },
        phoneNumber = "+49 30 123456",
        iban = "DE89 3704 0044 0532 0130 00",
    };

    [Test]
    public async Task Post_Valid_Returns201WithContact()
    {
        var response = await _client.PostAsJsonAsync("/api/contacts", Body("Ada", "Lovelace"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(response.Headers.Location!.ToString(), Does.EndWith($"/api/contacts/{json.GetProperty("id").GetGuid()}"));
        Assert.That(json.GetProperty("dateOfBirth").GetString(), Is.EqualTo("1990-04-12"));
        Assert.That(json.GetProperty("iban").GetString(), Is.EqualTo("DE89370400440532013000"));
        Assert.That(json.GetProperty("ibanFormatted").GetString(), Is.EqualTo("DE89 3704 0044 0532 0130 00"));
        Assert.That(json.GetProperty("phoneNumber").GetString(), Is.EqualTo("+4930123456"));
        Assert.That(json.GetProperty("address").GetProperty("city").GetString(), Is.EqualTo("Berlin"));
        Assert.That(json.GetProperty("createdAt").GetString(), Does.EndWith("Z"));
    }

    [Test]
    public async Task Post_Invalid_Returns400WithCamelCaseNestedKeys()
    {
        var body = new
        {
            firstName = "",
            surname = "Lovelace",
            dateOfBirth = "1990-04-12",
            address = new { street = "Main St 1", postalCode = "10115", city = "", country = "Germany" },
            phoneNumber = "abc",
            iban = "DE00000000000000000000",
        };

        var response = await _client.PostAsJsonAsync("/api/contacts", body);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var keys = json.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToList();
        Assert.That(keys, Is.SupersetOf(new[] { "firstName", "address.city", "phoneNumber", "iban" }));
    }

    [Test]
    public async Task Get_ReturnsContactsOrderedBySurnameThenFirstNameCaseInsensitive()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        await _client.PostAsJsonAsync("/api/contacts", Body("Zed", $"zz{tag}"));
        await _client.PostAsJsonAsync("/api/contacts", Body("bob", $"ZZ{tag}"));
        await _client.PostAsJsonAsync("/api/contacts", Body("Alice", $"zz{tag}"));

        var response = await _client.GetAsync("/api/contacts");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var list = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        var names = list!
            .Where(c => c.GetProperty("surname").GetString()!.Equals($"zz{tag}", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.GetProperty("firstName").GetString())
            .ToList();
        Assert.That(names, Is.EqualTo(new[] { "Alice", "bob", "Zed" }));
    }
}
