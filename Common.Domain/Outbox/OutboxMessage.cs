using System.Text.Json;

namespace Modules.Common.Domain.Outbox;

/// <summary>
/// Outbox message - stores events in same transaction as domain changes
/// Guarantees at-least-once delivery of domain events
/// 
/// WHY: If we publish events directly, and DB commit fails, 
/// downstream systems get notified but our DB state is inconsistent.
/// 
/// SOLUTION: Save events to outbox table in same transaction,
/// then background job publishes them.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private init; }
    
    /// <summary>
    /// Event type (e.g., "TransactionCompleted", "TransactionFailed")
    /// </summary>
    public string Type { get; private set; } = null!;
    
    /// <summary>
    /// Serialized event payload (JSON)
    /// </summary>
    public string Payload { get; private set; } = null!;
    
    /// <summary>
    /// When event was created (inserted into outbox)
    /// </summary>
    public DateTime CreatedAt { get; private init; }
    
    /// <summary>
    /// When event was successfully published
    /// null = not yet processed
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }
    
    /// <summary>
    /// Number of times we tried to publish this event
    /// </summary>
    public int ProcessingAttempts { get; private set; }
    
    /// <summary>
    /// Last error when trying to publish
    /// </summary>
    public string? LastError { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create<TEvent>(TEvent @event) where TEvent : class
    {
        var eventType = @event.GetType().Name;
        var payload = JsonSerializer.Serialize(@event, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = eventType,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            ProcessingAttempts = 0
        };
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void IncrementAttempts(string? error = null)
    {
        ProcessingAttempts++;
        LastError = error;
    }

    public bool ShouldRetry(int maxAttempts = 5)
    {
        return ProcessingAttempts < maxAttempts && ProcessedAt is null;
    }
}
