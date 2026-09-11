using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Infrastructure.Messaging;

/// <summary>Polls unprocessed OutboxMessage rows and relays them onto the bus via MassTransit's
/// IPublishEndpoint (research.md item 1/2). Runs against whatever transport DependencyInjection registered —
/// RabbitMQ when configured, otherwise MassTransit's in-memory transport as a local-dev fallback.</summary>
public sealed class OutboxDispatcherHostedService(
    IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatch loop failed");
            }

            await Task.Delay(PollInterval, stoppingToken).ContinueWith(_ => { });
        }
    }

    private async Task DispatchPendingAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(50)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        foreach (var message in pending)
        {
            try
            {
                var eventType = Type.GetType(message.Type)
                    ?? throw new InvalidOperationException($"Unknown outbox event type '{message.Type}'.");
                var payload = JsonSerializer.Deserialize(message.Content, eventType)
                    ?? throw new InvalidOperationException("Outbox payload deserialized to null.");

                await publishEndpoint.Publish(payload, eventType, ct);
                message.MarkProcessed();
                logger.LogInformation("Dispatched outbox message {MessageId} of type {Type}", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                logger.LogError(ex, "Failed to dispatch outbox message {MessageId}", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
