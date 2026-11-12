using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Integration.Tests;

[TestFixture]
public class HealthEndpointTests : IntegrationTestBase
{
    [Test]
    public async Task Get_ReturnsOkStatus()
    {
        var response = await Client.GetAsync("/api/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.That(body, Is.Not.Null.And.ContainKey("status").WithValue("ok"));
    }

    [Test]
    public async Task SwaggerUi_HasClientButton()
    {
        var html = await Client.GetStringAsync("/swagger/index.html");

        Assert.That(html, Does.Contain("topbar-links").And.Contain("Client"));
    }

    [Test]
    public async Task SwaggerJson_DescribesAllEndpoints()
    {
        var response = await Client.GetAsync("/swagger/v1/swagger.json");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var paths = json.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();

        Assert.That(paths, Is.SupersetOf(new[] { "/api/health", "/api/contacts", "/api/contacts/{id}" }));
    }
}
