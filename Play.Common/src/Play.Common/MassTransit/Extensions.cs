using System.Reflection;
using GreenPipes;
using MassTransit;
using MassTransit.Definition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Play.Common.Settings;

namespace Play.Common.MassTransit;

public static class Extensions
{
    public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services)
    {
        services.AddMassTransit(configure => 
        {
            configure.AddConsumers(Assembly.GetEntryAssembly());

            configure.UsingRabbitMq((context, cfg) => {
                var configuration = context.GetService<IConfiguration>();
                var serviceSettings = configuration?.GetSection(nameof(ServiceSettings))
                    .Get<ServiceSettings>();

                var rabbitMQSettings = configuration?
                .GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
                cfg.Host(rabbitMQSettings?.Host);
                cfg.ConfigureEndpoints(
                    context, 
                    new KebabCaseEndpointNameFormatter(serviceSettings?.ServiceName, false));
                cfg.UseMessageRetry(retryConfig => {
                    retryConfig.Interval(3, TimeSpan.FromSeconds(5));
                });

            });
        });

        services.AddMassTransitHostedService();

        return services;
    }
}