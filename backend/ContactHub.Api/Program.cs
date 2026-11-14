using ContactHub.Api.Extensions;
using ContactHub.Api.Middleware;
using ContactHub.Application;
using ContactHub.Infrastructure;
using ContactHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace ContactHub.Api;

// WebApplication hosting with MVC controllers (no Minimal API endpoints).
public class Program
{
    public static void Main(string[] args)
    {
        var app = BuildApp(args);

        app.Run();
    }

    public static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        AddServices(builder.Services);

        var app = builder.Build();
        PrepareDatabase(app);
        UsePipeline(app);

        return app;
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<ConflictExceptionHandler>();
        services.AddSingleton(TimeProvider.System);
        services.AddApplication();
        services.AddInfrastructure();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o => o.SwaggerDoc("v1", new OpenApiInfo { Title = "ContactHub API", Version = "v1" }));
    }

    private static void PrepareDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        if (app.Configuration.GetValue<bool>("SeedDemoData"))
        {
            DemoDataSeeder.Seed(db, scope.ServiceProvider.GetRequiredService<TimeProvider>());
        }
    }

    private static void UsePipeline(WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint("/swagger/v1/swagger.json", "ContactHub API v1");
            SwaggerTopBar.AddTopBarLinks(o, app.Configuration);
        });

        app.UseAuthorization();
        app.MapControllers();
    }
}
