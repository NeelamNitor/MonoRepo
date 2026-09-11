using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.ExternalServices;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrderDb")
            ?? "Host=localhost;Port=5432;Database=order_db;Username=postgres;Password=postgres";

        services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(connectionString));

        // Scoped: DbContext-backed implementations must live per-request (research.md item 9 — never Singleton).
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();

        services.AddTransient<ServiceAuthDelegatingHandler>();

        var productServiceBaseUrl = configuration["Services:ProductService:BaseUrl"] ?? "http://localhost:5001/";
        services.AddHttpClient<IProductServiceClient, ProductServiceHttpClient>(client =>
        {
            client.BaseAddress = new Uri(productServiceBaseUrl);
        }).AddHttpMessageHandler<ServiceAuthDelegatingHandler>();

        services.AddOrderMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddOrderMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var useRabbitMq = Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<bool>(configuration, "Messaging:UseRabbitMq");

        services.AddMassTransit(x =>
        {
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
                // Local-dev fallback when no broker is provisioned — see ProductService.Infrastructure's
                // equivalent registration for the full explanation (research.md item 1 / docker-compose.yml).
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            }
        });

        services.AddHostedService<OutboxDispatcherHostedService>();

        return services;
    }
}
