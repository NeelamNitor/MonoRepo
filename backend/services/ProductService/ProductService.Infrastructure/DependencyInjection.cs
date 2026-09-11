using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Messaging;
using ProductService.Infrastructure.Persistence;
using ProductService.Infrastructure.Repositories;

namespace ProductService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ProductDb")
            ?? "Host=localhost;Port=5432;Database=product_db;Username=postgres;Password=postgres";

        services.AddDbContext<ProductDbContext>(options => options.UseNpgsql(connectionString));

        // Scoped: DbContext-backed implementations must live per-request (research.md item 9 — never Singleton).
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();

        services.AddProductMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddProductMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var useRabbitMq = Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<bool>(configuration, "Messaging:UseRabbitMq");

        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderCancelledConsumer>();

            if (useRabbitMq)
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    var host = configuration["Messaging:RabbitMq:Host"] ?? "localhost";
                    var username = configuration["Messaging:RabbitMq:Username"] ?? "guest";
                    var password = configuration["Messaging:RabbitMq:Password"] ?? "guest";
                    cfg.Host(host, "/", h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                // Local-dev fallback when no broker is provisioned (no Docker/RabbitMQ in this environment) —
                // the outbox still records every event durably; only cross-process delivery is unavailable
                // until Messaging:UseRabbitMq is switched on against a real broker (see docker-compose.yml).
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            }
        });

        services.AddHostedService<OutboxDispatcherHostedService>();

        return services;
    }
}
