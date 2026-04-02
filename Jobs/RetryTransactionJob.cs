using Microsoft.EntityFrameworkCore;
using TransactionalBusiness.Api.Data;
using TransactionalBusiness.Api.Services;
using TransactionalBusiness.Api.Domain;
using Serilog;

namespace TransactionalBusiness.Api.Jobs;
public class RetryTransactionJob
{
     private readonly ITransactionService _service;
    private readonly PaymentDbContext _db;



    public RetryTransactionJob(ITransactionService service, PaymentDbContext db)
    {
        _service = service;
        _db = db;
    }

    public async Task ExecuteAsync(Guid transactionId
)
    {
         var transaction = await _db.Transactions.FirstOrDefaultAsync(t
         => t.Id == transactionId
);
         if(transaction == null) return;

             Log.Information("Retrying transaction {Id}, attempt {Count}", 
    transactionId, transaction.RetryCount);

         if(transaction.Status != TransactionStatus.RetryScheduled)
         return;
        try
        {
            await _service.ProcessAsync(transactionId
);
            await _service.CompleteAsync(transactionId
);
        }
        catch(Exception ex)
        {
               if (FailureClassifier.IsPermanent(ex.Message))
    {
        // permanent — don't retry
        transaction.PermanentFail(ex.Message);
        await _db.SaveChangesAsync();
    }
    else if (FailureClassifier.IsTransient(ex.Message))
    {
        // transient — schedule retry
        var nextRetry = DateTime.UtcNow.AddSeconds(
            Math.Pow(2, transaction.RetryCount) * 30);
        transaction.ScheduleRetry(ex.Message, nextRetry);
        await _db.SaveChangesAsync();
    }
    else
    {
        // unknown — treat as permanent
        transaction.PermanentFail(ex.Message);
        await _db.SaveChangesAsync();
    }
        }
    }



}