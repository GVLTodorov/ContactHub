using Api.Application.Abstractions;
using Api.Application.Behaviors;
using FluentValidation;

namespace Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddValidatorsFromAssembly(
            assembly,
            filter: r => r.ValidatorType.Namespace?.StartsWith("Api.Application", StringComparison.Ordinal) == true);

        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var iface in type.GetInterfaces().Where(i => i.IsGenericType))
            {
                var definition = iface.GetGenericTypeDefinition();
                if (definition == typeof(IQueryHandler<,>))
                {
                    services.AddScoped(iface, type);
                }
                else if (definition == typeof(ICommandHandler<,>))
                {
                    var decorator = typeof(ValidationCommandHandlerDecorator<,>).MakeGenericType(iface.GetGenericArguments());
                    var handlerType = type;
                    services.AddScoped(handlerType);
                    services.AddScoped(iface, sp =>
                        ActivatorUtilities.CreateInstance(sp, decorator, sp.GetRequiredService(handlerType)));
                }
            }
        }

        return services;
    }
}
