using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ContactHub.Api.Integration.Tests;

[TestFixture]
public class ContactsEndpointTests : IntegrationTestBase
{
    [Test]
    public async Task Post_Valid_Returns201WithContact()
    {
        var response = await Client.PostAsJsonAsync("/api/contacts", ValidBody());

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
    public async Task Post_NormalisesValues_AndTheyPersistAcrossGet()
    {
        var body = ValidBody("  Ada  ", "  Lovelace ");
        body["phoneNumber"] = "+49 (30) 123-456";
        body["iban"] = "de89 3704 0044 0532 0130 00";
        body["address"]!["street"] = "  Main St 1  ";
        var created = await CreateContactAsync(body);
        var id = created.GetProperty("id").GetGuid();

        var fetched = await Client.GetFromJsonAsync<JsonElement>($"/api/contacts/{id}");

        Assert.That(fetched.GetProperty("firstName").GetString(), Is.EqualTo("Ada"));
        Assert.That(fetched.GetProperty("surname").GetString(), Is.EqualTo("Lovelace"));
        Assert.That(fetched.GetProperty("phoneNumber").GetString(), Is.EqualTo("+4930123456"));
        Assert.That(fetched.GetProperty("iban").GetString(), Is.EqualTo("DE89370400440532013000"));
        Assert.That(fetched.GetProperty("ibanFormatted").GetString(), Is.EqualTo("DE89 3704 0044 0532 0130 00"));
        Assert.That(fetched.GetProperty("address").GetProperty("street").GetString(), Is.EqualTo("Main St 1"));
    }

    [Test]
    public async Task Post_FieldsAtMaxLength_AreAccepted()
    {
        var body = ValidBody(new string('a', 100), new string('b', 100));
        body["address"]!["street"] = new string('s', 200);
        body["address"]!["postalCode"] = new string('p', 20);
        body["address"]!["city"] = new string('c', 100);
        body["address"]!["country"] = new string('k', 100);

        var response = await Client.PostAsJsonAsync("/api/contacts", body);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task Post_DateOfBirthToday_IsAccepted()
    {
        var body = ValidBody();
        body["dateOfBirth"] = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await Client.PostAsJsonAsync("/api/contacts", body);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task Post_MalformedJson_Returns400()
    {
        using var content = new StringContent("{ \"firstName\": ", Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/contacts", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Put_MalformedJson_Returns400()
    {
        var id = await CreateContactIdAsync();
        using var content = new StringContent("not json", Encoding.UTF8, "application/json");

        var response = await Client.PutAsync($"/api/contacts/{id}", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Post_UnparseableDateOfBirth_Returns400()
    {
        var body = ValidBody();
        body["dateOfBirth"] = "not-a-date";

        var response = await Client.PostAsJsonAsync("/api/contacts", body);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Get_ReturnsContactsOrderedBySurnameThenFirstNameCaseInsensitive()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        await CreateContactIdAsync("Zed", $"zz{tag}");
        await CreateContactIdAsync("bob", $"ZZ{tag}");
        await CreateContactIdAsync("Alice", $"zz{tag}");
        await CreateContactIdAsync("Zoe", $"aa{tag}");

        var response = await Client.GetAsync("/api/contacts");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var list = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        var names = list!
            .Where(c => c.GetProperty("surname").GetString()!.EndsWith(tag, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.GetProperty("firstName").GetString())
            .ToList();

        Assert.That(names, Is.EqualTo(new[] { "Zoe", "Alice", "bob", "Zed" }));
    }

    [Test]
    public async Task GetById_Existing_Returns200()
    {
        var id = await CreateContactIdAsync();

        var response = await Client.GetAsync($"/api/contacts/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(json.GetProperty("id").GetGuid(), Is.EqualTo(id));
        Assert.That(json.GetProperty("dateOfBirth").GetString(), Is.EqualTo("1990-04-12"));
        Assert.That(json.GetProperty("ibanFormatted").GetString(), Is.EqualTo("DE89 3704 0044 0532 0130 00"));
        Assert.That(json.GetProperty("address").GetProperty("street").GetString(), Is.EqualTo("Main St 1"));
    }

    [Test]
    public async Task GetById_Unknown_Returns404ProblemDetails()
    {
        var response = await Client.GetAsync($"/api/contacts/{Guid.NewGuid()}");

        await AssertNotFoundProblemAsync(response);
    }

    [Test]
    public async Task Put_Valid_Returns200WithUpdatedContact()
    {
        var id = await CreateContactIdAsync();
        var body = ValidBody("Grace", "Hopper");
        body["phoneNumber"] = "+1 202 555 0143";

        var response = await Client.PutAsJsonAsync($"/api/contacts/{id}", body);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(json.GetProperty("id").GetGuid(), Is.EqualTo(id));
        Assert.That(json.GetProperty("firstName").GetString(), Is.EqualTo("Grace"));
        var fetched = await Client.GetFromJsonAsync<JsonElement>($"/api/contacts/{id}");

        Assert.That(fetched.GetProperty("surname").GetString(), Is.EqualTo("Hopper"));
        Assert.That(fetched.GetProperty("phoneNumber").GetString(), Is.EqualTo("+12025550143"));
    }

    [Test]
    public async Task Put_KeepsCreatedAtUnchanged_AndMovesUpdatedAtForward()
    {
        var created = await CreateContactAsync();
        var id = created.GetProperty("id").GetGuid();
        var createdAt = created.GetProperty("createdAt").GetDateTime();

        var response = await Client.PutAsJsonAsync($"/api/contacts/{id}", ValidBody("Grace", "Hopper"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
        var fetched = await Client.GetFromJsonAsync<JsonElement>($"/api/contacts/{id}");

        Assert.That(updated.GetProperty("createdAt").GetDateTime(), Is.EqualTo(createdAt));
        Assert.That(fetched.GetProperty("createdAt").GetDateTime(), Is.EqualTo(createdAt));
        Assert.That(fetched.GetProperty("updatedAt").GetDateTime(), Is.GreaterThan(createdAt));
    }

    [Test]
    public async Task Put_ReturnsNextVersion_AndStaleVersionGets409()
    {
        var id = await CreateContactIdAsync();

        var first = await Client.PutAsJsonAsync($"/api/contacts/{id}", ValidBody("Grace", "Hopper"));
        var firstJson = await first.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(firstJson.GetProperty("version").GetInt32(), Is.EqualTo(2));

        var stale = await Client.PutAsJsonAsync($"/api/contacts/{id}", ValidBody("Other", "Writer"));
        var problem = await stale.Content.ReadFromJsonAsync<JsonElement>();
        var fetched = await Client.GetFromJsonAsync<JsonElement>($"/api/contacts/{id}");

        Assert.That(stale.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo(409));
        Assert.That(problem.GetProperty("detail").GetString(), Does.Contain("changed by someone else"));
        Assert.That(fetched.GetProperty("firstName").GetString(), Is.EqualTo("Grace"));
    }

    [Test]
    public async Task Put_WithoutVersion_Returns400()
    {
        var id = await CreateContactIdAsync();
        var body = ValidBody();
        body.Remove("version");

        var response = await Client.PutAsJsonAsync($"/api/contacts/{id}", body);
        var keys = await ErrorKeysAsync(response);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(keys, Is.EqualTo(new[] { "version" }));
    }

    [Test]
    public async Task Put_Unknown_Returns404ProblemDetails()
    {
        var response = await Client.PutAsJsonAsync($"/api/contacts/{Guid.NewGuid()}", ValidBody());

        await AssertNotFoundProblemAsync(response);
    }

    [Test]
    public async Task Delete_Existing_Returns204AndContactIsGone()
    {
        var id = await CreateContactIdAsync();

        var response = await Client.DeleteAsync($"/api/contacts/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var list = await Client.GetFromJsonAsync<JsonElement>("/api/contacts");
        var ids = list.EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList();
        var get = await Client.GetAsync($"/api/contacts/{id}");

        Assert.That(ids, Does.Not.Contain(id));
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_Unknown_Returns404ProblemDetails()
    {
        var response = await Client.DeleteAsync($"/api/contacts/{Guid.NewGuid()}");

        await AssertNotFoundProblemAsync(response);
    }

    [Test]
    public async Task Delete_Twice_SecondReturns404()
    {
        var id = await CreateContactIdAsync();
        await Client.DeleteAsync($"/api/contacts/{id}");

        var response = await Client.DeleteAsync($"/api/contacts/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task NonGuidId_Returns404ForGetPutDelete()
    {
        var get = await Client.GetAsync("/api/contacts/not-a-guid");
        var put = await Client.PutAsJsonAsync("/api/contacts/not-a-guid", ValidBody());
        var delete = await Client.DeleteAsync("/api/contacts/not-a-guid");

        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(put.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static async Task AssertNotFoundProblemAsync(HttpResponseMessage response)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(json.GetProperty("status").GetInt32(), Is.EqualTo(404));
        Assert.That(json.TryGetProperty("title", out _), Is.True);
    }
}
