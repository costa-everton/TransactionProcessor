using Microsoft.AspNetCore.Mvc;
using TransactionProcessor.Models;

namespace TransactionProcessor.Api;

public static class TransactionHttpMapper
{
    public static IActionResult ToHttpResponse(TransactionRecord tr)
    {
        // Se a transação foi bem-sucedida
        if (tr.Status == TransactionStatus.Success)
        {
            return new OkObjectResult(tr);
        }

        // Retorna BadRequest para erros conhecidos
        return tr.ErrorCode switch
        {
            "error.invalid_account"                      => BadRequest(tr),
            "error.insufficient_funds"                   => BadRequest(tr),
            "error.insufficient_available_for_reserve"  => BadRequest(tr),
            "error.insufficient_reserved"                => BadRequest(tr),
            "error.destination_account_required"         => BadRequest(tr),
            "error.original_reference_required"          => BadRequest(tr),
            "error.original_transaction_not_found"       => BadRequest(tr),
            "error.unknown_operation"                    => BadRequest(tr),
            "error.duplicate_reference"                  => BadRequest(tr),
            "error.exception"                            => new ObjectResult(new { errorCode = tr.ErrorCode, errorMessage = tr.ErrorMessage }) { StatusCode = 500 },
            _                                            => new ObjectResult(new { error = "error.unknown" }) { StatusCode = 500 }
        };
    }

    private static BadRequestObjectResult BadRequest(TransactionRecord tr) =>
        new BadRequestObjectResult(new { errorCode = tr.ErrorCode, errorMessage = tr.ErrorMessage });
}
