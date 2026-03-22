using Microsoft.AspNetCore.Mvc;
using TransactionalBusiness.Api.Services;
using TransactionalBusiness.Api.Models;
using TransactionalBusiness.Api.Domain;

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


        var response = new TransactionResponse(
           Id = transaction.Id,
           userId = transaction.UserId
        );
    }




}

