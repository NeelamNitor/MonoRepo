namespace OrderService.Infrastructure.Messaging;

/// <summary>Transactional outbox row (research.md item 2). Written in the same DbContext SaveChanges call as
/// the domain change that raised it, then relayed to the bus by OutboxDispatcherHostedService.</summary>
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { } // EF Core

    public OutboxMessage(string type, string content)
    {
        Id = Guid.NewGuid();
        Type = type;
        Content = content;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public void MarkProcessed() => ProcessedAt = DateTimeOffset.UtcNow;

    public void MarkFailed(string error) => Error = error;
}
