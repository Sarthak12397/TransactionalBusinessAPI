using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Results;
using Modules.Transactions.Domain.Entities;
using Polly;
using Polly.Retry;

namespace Modules.Transactions.Infrastructure.PaymentProcessors;

/// <summary>
/// Service for calling external payment processors (Stripe, etc.)
/// </summary>
public interface IPaymentProcessorService
{
    Task<Result<ProcessorResponse>> ProcessPaymentAsync(
        Transaction transaction,
        CancellationToken cancellationToken);

    Task<Result<ProcessorResponse>> RefundPaymentAsync(
        Transaction transaction,
        CancellationToken cancellationToken);
}

/// <summary>
/// Response from payment processor
/// </summary>
public record ProcessorResponse(
    string TransactionId,
    string ResponseCode,
    string? RawResponse = null
);

/// <summary>
/// Stripe implementation (example)
/// Replace with your actual payment processor
/// </summary>
public sealed class StripePaymentProcessorService : IPaymentProcessorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StripePaymentProcessorService> _logger;
    private readonly AsyncRetryPolicy<Result<ProcessorResponse>> _retryPolicy;

    public StripePaymentProcessorService(
        HttpClient httpClient,
        ILogger<StripePaymentProcessorService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configure Polly retry policy
        _retryPolicy = Policy<Result<ProcessorResponse>>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult(r => r.IsFailure && r.Errors.Any(e => e.Type == ErrorType.Transient))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s. Reason: {Reason}",
                        retryCount,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? "Result indicated retry");
                });
    }

    public async Task<Result<ProcessorResponse>> ProcessPaymentAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Calling Stripe to process transaction {TransactionId} for amount {Amount} {Currency}",
            transaction.Id,
            transaction.Amount.Amount,
            transaction.Currency);

        // Wrap in retry policy
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                // TODO: Replace with actual Stripe SDK call
                // Example:
                // var options = new ChargeCreateOptions
                // {
                //     Amount = transaction.Amount.AmountInCents,
                //     Currency = transaction.Currency.ToLower(),
                //     Source = transaction.PaymentMethod,
                //     Description = transaction.Description,
                //     IdempotencyKey = transaction.IdempotencyKey
                // };
                //
                // var service = new ChargeService();
                // var charge = await service.CreateAsync(options, cancellationToken: cancellationToken);

                // MOCK IMPLEMENTATION - Replace this!
                await Task.Delay(100, cancellationToken); // Simulate API call

                // Simulate 10% failure rate for testing
                var random = new Random();
                if (random.Next(100) < 10)
                {
                    return Error.Transient(
                        "Stripe.NetworkError",
                        "Connection timeout to Stripe API");
                }

                var mockChargeId = $"ch_{Guid.NewGuid():N}";
                
                return new ProcessorResponse(
                    TransactionId: mockChargeId,
                    ResponseCode: "success",
                    RawResponse: "{\"status\": \"succeeded\"}"
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling Stripe");
                return Error.Transient("Stripe.NetworkError", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling Stripe");
                return Error.Failure("Stripe.UnexpectedError", ex.Message);
            }
        });
    }

    public async Task<Result<ProcessorResponse>> RefundPaymentAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Calling Stripe to refund transaction {TransactionId}",
            transaction.Id);

        // Similar implementation for refunds
        // TODO: Implement actual Stripe refund call

        await Task.Delay(100, cancellationToken);

        var mockRefundId = $"re_{Guid.NewGuid():N}";

        return new ProcessorResponse(
            TransactionId: mockRefundId,
            ResponseCode: "refunded",
            RawResponse: "{\"status\": \"refunded\"}"
        );
    }
}

/// <summary>
/// Custom error types for payment processing
/// </summary>
public static class ErrorType
{
    public const string Transient = "Transient"; // Temporary - retry
    public const string Failure = "Failure";     // Permanent - don't retry
}
