using System.Diagnostics;
using ContactHub.Api.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContactHub.Api.Tests.Api;

[TestFixture]
public class ProgramTests
{
    private sealed class HostCapture : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        private IDisposable? _subscription;

        public IHost? Host { get; private set; }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name == "Microsoft.Extensions.Hosting")
            {
                _subscription = value.Subscribe(this);
            }
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == "HostBuilt" && value.Value is IHost host)
            {
                Host = host;
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void Stop() => _subscription?.Dispose();
    }

    [Test]
    public async Task BuildApp_RegistersServicesAndMigratesDatabase()
    {
        ApiFactoryTestBase.ResetTestDatabase();

        await using var app = Program.BuildApp(["--environment", "Testing"]);

        Assert.That(app.Services.GetService<TimeProvider>(), Is.Not.Null);
        Assert.That(File.Exists(Path.Combine(AppContext.BaseDirectory, "database", "contacthub_test.db")), Is.True);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }

    [Test]
    public async Task Main_StartsAndStopsGracefully()
    {
        ApiFactoryTestBase.ResetTestDatabase();
        var capture = new HostCapture();
        using var listenerSubscription = DiagnosticListener.AllListeners.Subscribe(capture);

        var main = Task.Run(() => Program.Main(["--environment", "Testing", "--urls", "http://127.0.0.1:0"]));

        var deadline = DateTime.UtcNow.AddSeconds(60);
        IHostApplicationLifetime? lifetime = null;
        while (DateTime.UtcNow < deadline && !main.IsCompleted)
        {
            lifetime = capture.Host?.Services.GetService<IHostApplicationLifetime>();
            if (lifetime is { ApplicationStarted.IsCancellationRequested: true })
            {
                break;
            }

            await Task.Delay(50);
        }

        capture.Stop();
        Assert.That(lifetime, Is.Not.Null, "host was never built");
        lifetime!.StopApplication();
        await main.WaitAsync(TimeSpan.FromSeconds(60));

        Assert.That(main.IsCompletedSuccessfully, Is.True);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }
}
