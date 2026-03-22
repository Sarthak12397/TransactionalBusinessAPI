namespace TransactionalBusiness.Api.Models;

public class CreateTransactionRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string Description { get; set; }
    public string IdempotencyKey { get; set; }

}