using TransactionalBusiness.Api.Domain;
namespace TransactionalBusiness.Api.Models;

public class TransactionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}