namespace ContactHub.E2E;

/// <summary>Starts the backend and frontend once for every e2e test, and stops them afterwards.</summary>
[SetUpFixture]
public class E2ESetup
{
    private static Servers? _servers;

    internal static Servers Servers => _servers ?? throw new InvalidOperationException("Servers are not started.");

    [OneTimeSetUp]
    public async Task StartServers()
    {
        _servers = await Servers.StartAsync("contacthub_e2e.db", seedDemoData: false);
    }

    [OneTimeTearDown]
    public void StopServers()
    {
        _servers?.Dispose();
        _servers = null;
    }
}
