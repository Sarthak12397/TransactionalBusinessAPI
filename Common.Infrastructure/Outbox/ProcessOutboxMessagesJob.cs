using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Outbox;
using System.Text.Json;

namespace Modules.Common.Infrastructure.Outbox;

/// <summary>
/// Background job that processes outbox messages
/// Runs every 30 seconds to publish pending events
/// </summary>
public interface IProcessOutboxMessagesJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class ProcessOutboxMessagesJob(
    DbContext dbContext, // Generic - works with any module's DbContext that has OutboxMessages
    IEventPublisher eventPublisher,
    ILogger<ProcessOutboxMessagesJob> logger) 
    : IProcessOutboxMessagesJob
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 5;

    [Queue("default")]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Fetch pending outbox messages
        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null && m.ProcessingAttempts < MaxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            logger.LogDebug("No pending outbox messages to process");
            return;
        }

        logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Deserialize and publish event
                var eventType = Type.GetType(message.Type);
                if (eventType is null)
                {
                    logger.LogWarning("Unknown event type: {Type}", message.Type);
                    message.IncrementAttempts($"Unknown event type: {message.Type}");
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, eventType);
                if (@event is null)
                {
                    logger.LogWarning("Failed to deserialize event {MessageId}", message.Id);
                    message.IncrementAttempts("Deserialization failed");
                    continue;
                }

                // Publish to message bus / event handlers
                await eventPublisher.PublishAsync(@event, cancellationToken);

                message.MarkAsProcessed();
                
                logger.LogInformation(
                    "Published outbox message {MessageId} of type {Type}",
                    message.Id,
                    message.Type);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error processing outbox message {MessageId}",
                    message.Id);

                message.IncrementAttempts(ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // If there are more messages, schedule immediate follow-up
        var remainingCount = await dbContext.Set<OutboxMessage>()
            .CountAsync(m => m.ProcessedAt == null && m.ProcessingAttempts < MaxAttempts, cancellationToken);

        if (remainingCount > 0)
        {
            logger.LogInformation("{RemainingCount} outbox messages remaining, scheduling follow-up", remainingCount);
            BackgroundJob.Enqueue<IProcessOutboxMessagesJob>(
                job => job.ExecuteAsync(CancellationToken.None));
        }
    }
}

/// <summary>
/// Extension method to add outbox message in same transaction as domain changes
/// </summary>
public static class OutboxExtensions
{
    public static async Task AddOutboxMessageAsync<TEvent>(
        this DbContext dbContext,
        TEvent @event,
        CancellationToken cancellationToken = default) 
        where TEvent : class
    {
        var outboxMessage = OutboxMessage.Create(@event);
        await dbContext.Set<OutboxMessage>().AddAsync(outboxMessage, cancellationToken);
    }
}

/// <summary>
/// Simple event publisher interface
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : class;
}
