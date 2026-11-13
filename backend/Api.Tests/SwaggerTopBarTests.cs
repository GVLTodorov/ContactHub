using Api.Extensions;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Api.Tests;

[TestFixture]
public class SwaggerTopBarTests
{
    private static IConfiguration Config(string? clientUrl) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(clientUrl is null ? [] : new Dictionary<string, string?> { ["ClientUrl"] = clientUrl })
            .Build();

    [Test]
    public void AddTopBarLinks_UsesConfiguredClientUrl()
    {
        var options = new SwaggerUIOptions();

        SwaggerTopBar.AddTopBarLinks(options, Config("http://127.0.0.1:4700"));

        Assert.That(options.HeadContent, Does.Contain("<style>"));
        Assert.That(options.HeadContent, Does.Contain("http:\\u002F\\u002F127.0.0.1:4700").Or.Contain("http://127.0.0.1:4700"));
    }

    [Test]
    public void AddTopBarLinks_DefaultsClientUrlToRoot()
    {
        var options = new SwaggerUIOptions();

        SwaggerTopBar.AddTopBarLinks(options, Config(null));

        Assert.That(options.HeadContent, Does.Contain("[[\"Client\",\"/\"]]").Or.Contain("\"Client\",\"\\u002F\""));
    }
}
