using Api.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = new SqliteConnectionStringBuilder(
            configuration.GetConnectionString("Default") ?? "Data Source=database/contacthub.db");
        connectionString.DataSource = Path.GetFullPath(connectionString.DataSource, AppContext.BaseDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(connectionString.DataSource)!);

        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString.ToString()));

        return services;
    }
}
