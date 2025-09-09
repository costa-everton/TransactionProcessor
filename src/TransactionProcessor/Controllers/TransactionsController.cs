using Microsoft.AspNetCore.Mvc;
using TransactionProcessor.Services;
using TransactionProcessor.Api;
using TransactionProcessor.DTO;
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
        if (dto is null)
            return BadRequest("Transaction payload is required.");

        // Monta o TransactionCommand com OriginalReferenceId incluído (para reversals)
        var cmd = new TransactionCommand(
            Operation: dto.Operation,
            AccountId: dto.AccountId,
            Amount: dto.Amount,
            Currency: dto.Currency,
            ReferenceId: dto.ReferenceId,
            DestinationAccountId: dto.DestinationAccountId,
            OriginalReferenceId: dto.OriginalReferenceId,
            Metadata: dto.Metadata
        );

        // Chama o serviço
        var result = await _svc.ProcessAsync(cmd);

        // Retorna o TransactionRecord convertido para HTTP response
        return TransactionHttpMapper.ToHttpResponse(result);
    }
}
