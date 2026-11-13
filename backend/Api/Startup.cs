using Api.Extensions;
using Api.Middleware;
using ContactHub.Application;
using ContactHub.Infrastructure;
using ContactHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;

namespace Api;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddSingleton(TimeProvider.System);
        services.AddApplication();
        services.AddInfrastructure(Configuration);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o => o.SwaggerDoc("v1", new OpenApiInfo { Title = "ContactHub API", Version = "v1" }));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();

            if (Configuration.GetValue<bool>("SeedDemoData"))
            {
                DemoDataSeeder.Seed(db, scope.ServiceProvider.GetRequiredService<TimeProvider>());
            }
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint("/swagger/v1/swagger.json", "ContactHub API v1");
            SwaggerTopBar.AddTopBarLinks(o, Configuration);
        });

        var staticDir = Configuration["StaticDir"];
        PhysicalFileProvider? files = null;
        if (!string.IsNullOrEmpty(staticDir) && Directory.Exists(staticDir))
        {
            files = new PhysicalFileProvider(Path.GetFullPath(staticDir));
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
        }

        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            if (files is not null)
            {
                endpoints.MapFallbackToFile("{*path:regex(^(?!api(/|$)|swagger(/|$)).*$)}", "index.html", new StaticFileOptions { FileProvider = files })
                    .Add(b => b.Metadata.Add(new HttpMethodMetadata(["GET"])));
            }
        });
    }
}
