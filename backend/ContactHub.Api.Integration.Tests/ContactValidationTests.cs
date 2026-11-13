using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ContactHub.Api.Integration.Tests;

[TestFixture]
public class ContactValidationTests : IntegrationTestBase
{
    private const string Missing = "<missing>";

    private static readonly string Tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2).ToString("yyyy-MM-dd");
    private static readonly string TooOld = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-151).ToString("yyyy-MM-dd");

    // path, value (null = JSON null, Missing = omit, "len:N" = N chars), expected error key
    public static IEnumerable<TestCaseData> InvalidCases()
    {
        foreach (var field in new[] { "firstName", "surname", "phoneNumber", "iban", "address.street", "address.postalCode", "address.city", "address.country" })
        {
            yield return new TestCaseData(field, null, field).SetName($"{field}_Null");
            yield return new TestCaseData(field, Missing, field).SetName($"{field}_Missing");
            yield return new TestCaseData(field, "", field).SetName($"{field}_Empty");
            yield return new TestCaseData(field, "   ", field).SetName($"{field}_Whitespace");
        }

        yield return new TestCaseData("firstName", "len:101", "firstName").SetName("firstName_TooLong");
        yield return new TestCaseData("surname", "len:101", "surname").SetName("surname_TooLong");
        yield return new TestCaseData("address.street", "len:201", "address.street").SetName("street_TooLong");
        yield return new TestCaseData("address.postalCode", "len:21", "address.postalCode").SetName("postalCode_TooLong");
        yield return new TestCaseData("address.city", "len:101", "address.city").SetName("city_TooLong");
        yield return new TestCaseData("address.country", "len:101", "address.country").SetName("country_TooLong");

        yield return new TestCaseData("dateOfBirth", null, "dateOfBirth").SetName("dateOfBirth_Null");
        yield return new TestCaseData("dateOfBirth", Missing, "dateOfBirth").SetName("dateOfBirth_Missing");
        yield return new TestCaseData("dateOfBirth", Tomorrow, "dateOfBirth").SetName("dateOfBirth_InFuture");
        yield return new TestCaseData("dateOfBirth", TooOld, "dateOfBirth").SetName("dateOfBirth_TooOld");

        yield return new TestCaseData("address", null, "address").SetName("address_Null");
        yield return new TestCaseData("address", Missing, "address").SetName("address_Missing");

        yield return new TestCaseData("phoneNumber", "abc", "phoneNumber").SetName("phoneNumber_Letters");
        yield return new TestCaseData("phoneNumber", "123456", "phoneNumber").SetName("phoneNumber_TooShort");
        yield return new TestCaseData("phoneNumber", "+1234567890123456", "phoneNumber").SetName("phoneNumber_TooLong");

        yield return new TestCaseData("iban", "DE00000000000000000000", "iban").SetName("iban_BadChecksum");
        yield return new TestCaseData("iban", "12345", "iban").SetName("iban_BadFormat");
        yield return new TestCaseData("iban", "DE89", "iban").SetName("iban_TooShort");
    }

    [TestCaseSource(nameof(InvalidCases))]
    public async Task Post_Invalid_Returns400WithCamelCaseKey(string path, string? value, string expectedKey)
    {
        var body = Mutate(ValidBody(), path, value);

        var response = await Client.PostAsJsonAsync("/api/contacts", body);

        await AssertValidationProblemAsync(response, expectedKey);
    }

    [TestCaseSource(nameof(InvalidCases))]
    public async Task Put_Invalid_Returns400WithCamelCaseKey(string path, string? value, string expectedKey)
    {
        var id = await CreateContactIdAsync();
        var body = Mutate(ValidBody(), path, value);

        var response = await Client.PutAsJsonAsync($"/api/contacts/{id}", body);

        await AssertValidationProblemAsync(response, expectedKey);
    }

    [Test]
    public async Task Post_MultipleInvalidFields_ReportsAllNestedCamelCaseKeys()
    {
        var body = ValidBody();
        body["firstName"] = "";
        body["address"]!["city"] = "";
        body["phoneNumber"] = "abc";
        body["iban"] = "DE00000000000000000000";

        var response = await Client.PostAsJsonAsync("/api/contacts", body);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var keys = await ErrorKeysAsync(response);

        Assert.That(keys, Is.SupersetOf(new[] { "firstName", "address.city", "phoneNumber", "iban" }));
    }

    [Test]
    public async Task Put_MultipleInvalidFields_ReportsAllNestedCamelCaseKeys()
    {
        var id = await CreateContactIdAsync();
        var body = ValidBody();
        body["firstName"] = "";
        body["address"]!["city"] = "";

        var response = await Client.PutAsJsonAsync($"/api/contacts/{id}", body);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var keys = await ErrorKeysAsync(response);

        Assert.That(keys, Is.SupersetOf(new[] { "firstName", "address.city" }));
    }

    [Test]
    public async Task Put_Invalid_DoesNotChangeStoredContact()
    {
        var id = await CreateContactIdAsync("Ada", "Lovelace");
        var body = ValidBody("Grace", "Hopper");
        body["iban"] = "DE00000000000000000000";
        await Client.PutAsJsonAsync($"/api/contacts/{id}", body);

        var fetched = await Client.GetFromJsonAsync<JsonElement>($"/api/contacts/{id}");

        Assert.That(fetched.GetProperty("firstName").GetString(), Is.EqualTo("Ada"));
        Assert.That(fetched.GetProperty("surname").GetString(), Is.EqualTo("Lovelace"));
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string expectedKey)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var keys = json.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToList();

        Assert.That(json.GetProperty("status").GetInt32(), Is.EqualTo(400));
        Assert.That(keys, Does.Contain(expectedKey));
    }

    private static JsonObject Mutate(JsonObject body, string path, string? value)
    {
        var segments = path.Split('.');
        var target = body;
        foreach (var segment in segments[..^1])
        {
            target = target[segment]!.AsObject();
        }

        var leaf = segments[^1];
        if (value == Missing)
        {
            target.Remove(leaf);
        }
        else if (value is not null && value.StartsWith("len:", StringComparison.Ordinal))
        {
            target[leaf] = new string('x', int.Parse(value[4..]));
        }
        else
        {
            target[leaf] = value;
        }

        return body;
    }
}
