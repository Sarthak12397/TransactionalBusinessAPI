using Microsoft.AspNetCore.Mvc;
using TransactionalBusiness.Api.Models;
using TransactionalBusiness.Api.Domain;
using TransactionalBusiness.Api.Services;
namespace TransactionalBusiness.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionsController(ITransactionService service)
    {
        _service = service;
    }

    
    [HttpPost]
public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
    {
        
        var userId = Guid.NewGuid();

        var transaction = await _service.CreateAsync(
            userId,
            request.Amount,
            request.Currency,
            request.IdempotencyKey,
            request.Description
        );


        var response = new TransactionResponse
    {
        Id = transaction.Id,
        UserId = transaction.UserId,
        Amount = transaction.Amount,
        Currency = transaction.Currency,
        Status = transaction.Status,
        CreatedAt = transaction.CreatedAt
    };

    return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, response);
    }


 [HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
    {
        var transaction = await _service.GetByIdAsync(id);
        var response = new TransactionResponse
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Status = transaction.Status,
            CreatedAt = transaction.CreatedAt
        };
        return Ok(response);
    }

  [HttpPost("{id}/submit")]
   public async Task<IActionResult> Submit(Guid id)
    {
        await _service.SubmitAsync(id);
        return Ok();
    }
    [HttpPost("{id}/fail")]
    public async Task<IActionResult> Fail(Guid id)
    {
        await _service.FailAsync(id);
        return Ok();
    }
    
[HttpPost("{id}/process")]
public async Task<IActionResult> Process(Guid id)
{
    await _service.ProcessAsync(id);
    return Ok();
}

[HttpPost("{id}/complete")]
public async Task<IActionResult> Complete(Guid id)
{
    await _service.CompleteAsync(id);
    return Ok();
}


}






