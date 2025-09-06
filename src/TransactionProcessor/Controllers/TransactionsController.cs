using Microsoft.AspNetCore.Mvc;
using TransactionProcessor.Services;
using TransactionProcessor.Models;

namespace TransactionProcessor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _svc;
    public TransactionsController(ITransactionService svc) => _svc = svc;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] TransactionCommandDto dto)
    {
        var cmd = new TransactionCommand(dto.Operation, dto.AccountId, dto.Amount, dto.Currency, dto.ReferenceId, dto.DestinationAccountId, dto.Metadata);
        var result = await _svc.ProcessAsync(cmd);
        if (result.Status == "success") return CreatedAtAction(null, new { id = result.TransactionId }, result);
        return BadRequest(result);
    }
}

public record TransactionCommandDto(string Operation, string AccountId, long Amount, string Currency, string ReferenceId, string? DestinationAccountId = null, Dictionary<string,string>? Metadata = null);
