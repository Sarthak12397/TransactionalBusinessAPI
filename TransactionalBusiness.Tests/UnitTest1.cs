

using Xunit;
using FluentAssertions;
using TransactionalBusiness.Api.Domain;

namespace TransactionalBusiness.Tests;

public class TransactionTests
{
    private Transaction CreateTestTransaction()
    {
        return new Transaction(
            Guid.NewGuid(),
            100.00m,
            "NZD",
            "test-key-001",
            "Test payment"
        );
    }

    [Fact]
    public void Transaction_CreatedWithPendingStatus()
    {
        var transaction = CreateTestTransaction();
        transaction.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public void Submit_FromPending_ChangesStatusToSubmitted()
    {
        var transaction = CreateTestTransaction();
        transaction.Submit();
        transaction.Status.Should().Be(TransactionStatus.Submitted);
    }

    [Fact]
    public void Submit_FromProcessing_ThrowsInvalidOperationException()
    {
        var transaction = CreateTestTransaction();
        transaction.Submit();
        transaction.Process();

        Action act = () => transaction.Submit();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot Submit*");
    }

    [Fact]
    public void ScheduleRetry_WhenRetriesExhausted_SetsPermanentFailure()
    {
        var transaction = CreateTestTransaction();
        transaction.Submit();
        transaction.Process();
        transaction.ScheduleRetry("Network timeout", DateTime.UtcNow.AddSeconds(30));
        transaction.Process();
        transaction.ScheduleRetry("Network timeout", DateTime.UtcNow.AddSeconds(30));
        transaction.Process();
        transaction.ScheduleRetry("Network timeout", DateTime.UtcNow.AddSeconds(30));

        transaction.Status.Should().Be(TransactionStatus.Permanentfailure);
    }

    [Fact]
    public void Complete_FromProcessing_ChangesStatusToCompleted()
    {
        var transaction = CreateTestTransaction();
        transaction.Submit();
        transaction.Process();
        transaction.Complete();
        transaction.Status.Should().Be(TransactionStatus.Completed);
    }

    [Fact]
    public void Fail_WithPermanentReason_SetsPermanentFailure()
    {
        var transaction = CreateTestTransaction();
        transaction.Submit();
        transaction.Process();
        transaction.PermanentFail("Insufficient funds");
        transaction.Status.Should().Be(TransactionStatus.Permanentfailure);
        transaction.FailureReason.Should().Be("Insufficient funds");
    }

    [Fact]
    public void Transaction_WithZeroAmount_ThrowsArgumentException()
    {
        Action act = () => new Transaction(
            Guid.NewGuid(),
            0m,
            "NZD",
            "test-key-002",
            "Test"
        );
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Amount*");
    }



    [Fact]
public void ScheduleRetry_TransientFailure_SetsRetryScheduled()
{
    var transaction = CreateTestTransaction();
    transaction.Submit();
    transaction.Process();
    
    var nextRetry = DateTime.UtcNow.AddSeconds(30);
    transaction.ScheduleRetry("Network timeout", nextRetry);
    
    transaction.Status.Should().Be(TransactionStatus.RetryScheduled);
    transaction.NextRetryAt.Should().NotBeNull();
    transaction.FailureReason.Should().Be("Network timeout");
}

[Fact]
public void ScheduleRetry_ExponentialBackoff_IncreasesDelay()
{
    var transaction = CreateTestTransaction();
    transaction.Submit();
    transaction.Process();
    
    var firstRetry = DateTime.UtcNow.AddSeconds(30);
    transaction.ScheduleRetry("timeout", firstRetry);
    transaction.Process();
    
    var secondRetry = DateTime.UtcNow.AddSeconds(60);
    transaction.ScheduleRetry("timeout", secondRetry);
    
    transaction.NextRetryAt.Should().BeAfter(firstRetry);
}
[Fact]
public void Process_WhenAlreadyProcessing_ThrowsInvalidOperationException()
{
    var transaction = CreateTestTransaction();
    transaction.Submit();
    transaction.Process();

    // Second worker tries to process — should be rejected
    Action act = () => transaction.Process();
    act.Should().Throw<InvalidOperationException>()
       .WithMessage("*Cannot Process*");
}

[Fact]
public void RetryUntilSuccess_CompletesAfterTwoFailures()
{
    var transaction = CreateTestTransaction();
    transaction.Submit();
    
    // Attempt 1 - fail
    transaction.Process();
    transaction.ScheduleRetry("timeout", DateTime.UtcNow.AddSeconds(30));
    
    // Attempt 2 - fail  
    transaction.Process();
    transaction.ScheduleRetry("timeout", DateTime.UtcNow.AddSeconds(60));
    
    // Attempt 3 - success
    transaction.Process();
    transaction.Complete();
    
    transaction.Status.Should().Be(TransactionStatus.Completed);
    transaction.RetryCount.Should().Be(3);
}

[Fact]
public void Reverse_FromCompleted_ChangesStatusToReversed()
{
    var transaction = CreateTestTransaction();
    transaction.Submit();
    transaction.Process();
    transaction.Complete();
    transaction.Reverse();
    
    transaction.Status.Should().Be(TransactionStatus.Reversed);
}
}