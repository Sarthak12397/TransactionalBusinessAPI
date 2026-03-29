using TransactionalBusiness.Api.Data;
using TransactionalBusiness.Api.Services;

public class retryTransactionJob
{
     private readonly ITransactionService _service;
    private readonly PaymentDbContext _db;



    public retryTransactionJob(ITransactionService service, PaymentDbContext db)
    {
        _service = service;
        _db = db;
    }

}