using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace ContactHub.Api.Integration.Tests;

[TestFixture]
public class SeedDemoDataTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void CreateFactory()
    {
        SqliteConnection.ClearAllPools();
        var dbFile = Path.Combine(AppContext.BaseDirectory, "database", "contacthub_seed_test.db");
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            if (File.Exists(dbFile + suffix))
            {
                File.Delete(dbFile + suffix);
            }
        }

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedDemoData"] = "true",
                ["ConnectionStrings:Default"] = "Data Source=database/contacthub_seed_test.db",
            }));
        });
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void DisposeFactory()
    {
        _client.Dispose();
        _factory.Dispose();
        SqliteConnection.ClearAllPools();
    }

    [Test]
    public async Task SeedDemoData_ExposesSeededContactsThroughApi()
    {
        var list = await _client.GetFromJsonAsync<JsonElement>("/api/contacts");
        var names = list.EnumerateArray().Select(e => e.GetProperty("surname").GetString()).ToList();

        Assert.That(names, Has.Count.EqualTo(5));
        Assert.That(names, Does.Contain("Lovelace"));
    }
}
