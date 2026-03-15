using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TeamFlow.Api.Tests.Infrastructure;

/// <summary>
/// Shared extension methods for replacing services and health checks in test WebApplicationFactories.
/// </summary>
internal static class ServiceCollectionTestExtensions
{
    public static void ReplaceService<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TInterface));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddScoped<TInterface, TImplementation>();
    }

    public static void ReplaceHealthCheck(this IServiceCollection services)
    {
        // Remove existing RabbitMQ health check registration and add a no-op one
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IHealthCheck) &&
            d.ImplementationType?.Name == "RabbitMqHealthCheck");
        if (descriptor is not null)
            services.Remove(descriptor);

        // Configure health checks to replace rabbitmq with always-healthy
        services.Configure<HealthCheckServiceOptions>(options =>
        {
            var rabbitCheck = options.Registrations.FirstOrDefault(r => r.Name == "rabbitmq");
            if (rabbitCheck is not null)
            {
                options.Registrations.Remove(rabbitCheck);
                options.Registrations.Add(new HealthCheckRegistration(
                    "rabbitmq",
                    _ => new AlwaysHealthyCheck(),
                    failureStatus: HealthStatus.Degraded,
                    tags: ["ready"]));
            }
        });
    }
}
