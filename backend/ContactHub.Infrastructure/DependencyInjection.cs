using ContactHub.Application.Abstractions;
using ContactHub.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContactHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Read from the built IConfiguration, not at registration time, so overrides added while
        // the host is built (WebApplicationFactory in tests) are honoured.
        services.AddSingleton(sp => DatabaseConnection.From(sp.GetRequiredService<IConfiguration>()));
        services.AddDbContext<AppDbContext>((sp, o) => o.UseSqlite(sp.GetRequiredService<DatabaseConnection>().ConnectionString));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}

/// <summary>The SQLite connection string, with a relative file resolved against the app's folder.</summary>
public sealed record DatabaseConnection(string ConnectionString)
{
    public static DatabaseConnection From(IConfiguration configuration)
    {
        var builder = new SqliteConnectionStringBuilder(
            configuration.GetConnectionString("Default") ?? "Data Source=database/contacthub.db");
        builder.DataSource = Path.GetFullPath(builder.DataSource, AppContext.BaseDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(builder.DataSource)!);

        return new DatabaseConnection(builder.ToString());
    }
}
