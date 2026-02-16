using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentSystem.Domain.Entities;
using PaymentSystem.Domain.ValueObjects;
using PaymentSystem.Infrastructure.Database;
using Hangfire;

namespace PaymentSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionsDbContext _dbContext;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        TransactionsDbContext dbContext,
        ILogger<TransactionsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new transaction
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating transaction with idempotency key: {IdempotencyKey}", 
            request.IdempotencyKey);

        // CRITICAL: Check for existing transaction with same idempotency key
        var existingTransaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(
                t => t.IdempotencyKey == request.IdempotencyKey, 
                cancellationToken);

        if (existingTransaction is not null)
        {
            _logger.LogInformation(
                "Transaction with idempotency key {IdempotencyKey} already exists. Returning existing.",
                request.IdempotencyKey);

            return Ok(MapToResponse(existingTransaction));
        }

        // Validate amount
        if (request.Amount <= 0)
        {
            return BadRequest(new { error = "Amount must be greater than zero" });
        }

        // Generate transaction number
        var transactionNumber = GenerateTransactionNumber();

        // Create Money value object
        var money = Money.FromAmount(request.Amount);

        // Create Transaction aggregate
        var transaction = Transaction.Create(
            transactionNumber: transactionNumber,
            idempotencyKey: request.IdempotencyKey,
            amount: money,
            currency: request.Currency,
            customerId: request.CustomerId,
            orderId: request.OrderId,
            paymentMethod: request.PaymentMethod,
            description: request.Description
        );

        // Save to database
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created transaction {TransactionId} with number {TransactionNumber}",
            transaction.Id,
            transaction.TransactionNumber);

        // TODO: Enqueue background job to process transaction
        // BackgroundJob.Enqueue<ProcessTransactionJob>(
        //     job => job.ExecuteAsync(transaction.Id, CancellationToken.None));

        return CreatedAtAction(
            nameof(GetTransaction),
            new { id = transaction.Id },
            MapToResponse(transaction));
    }

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction is null)
        {
            return NotFound(new { error = "Transaction not found" });
        }

        return Ok(MapToResponse(transaction));
    }

    /// <summary>
    /// List all transactions
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TransactionResponse>>> ListTransactions(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Transactions.AsQueryable();

        // Filter by status if provided
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TransactionStatus>(status, out var statusEnum))
        {
            query = query.Where(t => t.Status == statusEnum);
        }

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(100) // Limit to 100 for now
            .ToListAsync(cancellationToken);

        return Ok(transactions.Select(MapToResponse).ToList());
    }

    // Helper methods
    private static string GenerateTransactionNumber()
    {
        // Simple implementation - use timestamp + random
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Random.Shared.Next(1000, 9999);
        return $"TXN-{timestamp}-{random}";
    }

    private static TransactionResponse MapToResponse(Transaction transaction)
    {
        return new TransactionResponse(
            Id: transaction.Id,
            TransactionNumber: transaction.TransactionNumber,
            Amount: transaction.Amount.Amount,
            Currency: transaction.Currency,
            Status: transaction.Status.ToString(),
            CustomerId: transaction.CustomerId,
            OrderId: transaction.OrderId,
            PaymentMethod: transaction.PaymentMethod,
            Description: transaction.Description,
            ProcessorTransactionId: transaction.ProcessorTransactionId,
            AttemptCount: transaction.AttemptCount,
            FailureReason: transaction.FailureReason,
            CreatedAt: transaction.CreatedAt,
            UpdatedAt: transaction.UpdatedAt,
            CompletedAt: transaction.CompletedAt
        );
    }
}

// DTOs
public record CreateTransactionRequest(
    string IdempotencyKey,
    decimal Amount,
    string Currency,
    string CustomerId,
    string OrderId,
    string PaymentMethod,
    string? Description
);

public record TransactionResponse(
    Guid Id,
    string TransactionNumber,
    decimal Amount,
    string Currency,
    string Status,
    string CustomerId,
    string OrderId,
    string PaymentMethod,
    string? Description,
    string? ProcessorTransactionId,
    int AttemptCount,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? CompletedAt
);
