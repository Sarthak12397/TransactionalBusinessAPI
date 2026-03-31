
using Hangfire;
using Microsoft.EntityFrameworkCore;
using TransactionalBusiness.Api.Data;
using TransactionalBusiness.Api.Domain;
using TransactionalBusiness.Api.Jobs;
using TransactionalBusiness.Api.Services;

namespace TransactionalBusiness.Api.Services;
public class TransactionService : ITransactionService
{

    private readonly PaymentDbContext _db;


    public TransactionService(PaymentDbContext db)
    {
        _db = db;
    }
    public async Task<Transaction> CreateAsync(Guid userId, decimal amount, string currency, string idempotencyKey, string description)
    {

        var existing = await _db.Transactions.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
        if(existing != null)
        {
            return existing;
        }

        var transaction = new Transaction(  userId,
            amount,
            currency,
            idempotencyKey,
            description    
        );

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        return transaction;
    }

    public async Task FailAsync(Guid id,string reason)
    {

        var failbyId = await _db.Transactions.FirstOrDefaultAsync(t=> t.Id == id);

        if (failbyId == null)
        
            throw new KeyNotFoundException($"Transaction {id} not found");
       if (failureclassifier.IsPermanent(reason))
{
    failbyId.PermanentFail(reason);
    await _db.SaveChangesAsync();
    return; // no retry!
}

// transient — schedule retry
var nextRetry = DateTime.UtcNow.AddSeconds(30);
failbyId.ScheduleRetry(reason, nextRetry);
await _db.SaveChangesAsync();

BackgroundJob.Schedule<RetryTransactionJob>(
    job => job.ExecuteAsync(id),
    TimeSpan.FromSeconds(30)
);

          
    }

    public async Task<Transaction> GetByIdAsync(Guid id)
    {
        
              var getbyid = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id);
        if(getbyid == null)
        {
            throw new KeyNotFoundException($"Transaction {id} not found");

        }
        return getbyid;

    }

    public async Task SubmitAsync(Guid id)
    {
        
        var submitbyId = await _db.Transactions.FirstOrDefaultAsync(t=> t.Id == id);

        if(submitbyId == null)
        {
                        throw new KeyNotFoundException($"No {id} submitted");

        }

        submitbyId.Submit();


        await  _db.SaveChangesAsync() ;                

    }


       public async Task ProcessAsync(Guid id)
            {

    var updated = await _db.Transactions
        .Where(t => t.Id == id 
               && (t.Status == TransactionStatus.Submitted 
               || t.Status == TransactionStatus.RetryScheduled))
        .ExecuteUpdateAsync(s => s
            .SetProperty(t => t.Status, TransactionStatus.Processing)
            .SetProperty(t => t.LastAttemptAt, DateTime.UtcNow)
            .SetProperty(t => t.NextRetryAt, (DateTime?)null)
            .SetProperty(t => t.UpdatedAt, DateTime.UtcNow));

    if (updated == 0) return;

           var processbyid = await _db.Transactions.FirstOrDefaultAsync(t=> t.Id == id);
           if(processbyid == null)
        {
             throw new KeyNotFoundException($"No {id} submitted");

        }

             processbyid.RecordAttempt();
            await _db.SaveChangesAsync();
        }public async Task CompleteAsync(Guid id)
{
    // Use ExecuteUpdateAsync — same as ProcessAsync
    var updated = await _db.Transactions
        .Where(t => t.Id == id 
               && t.Status == TransactionStatus.Processing)
        .ExecuteUpdateAsync(s => s
            .SetProperty(t => t.Status, TransactionStatus.Completed)
            .SetProperty(t => t.FailureReason, (string?)null)
            .SetProperty(t => t.UpdatedAt, DateTime.UtcNow));

    if (updated == 0)
        throw new InvalidOperationException($"Cannot complete transaction {id}");
}

}