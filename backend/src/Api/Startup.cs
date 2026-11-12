using Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace Api;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o => o.SwaggerDoc("v1", new OpenApiInfo { Title = "ContactHub API", Version = "v1" }));

        var connectionString = new SqliteConnectionStringBuilder(
            Configuration.GetConnectionString("Default") ?? "Data Source=database/contacthub.db");
        connectionString.DataSource = Path.GetFullPath(connectionString.DataSource, AppContext.BaseDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(connectionString.DataSource)!);

        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString.ToString()));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "ContactHub API v1"));
        }

        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
