using Microsoft.AspNetCore.Mvc;
using TransactionProcessor.Models;

namespace TransactionProcessor.Api;

public static class AccountHttpMapper
{
    public static IActionResult ToHttpResponse(AccountRecord ac)
    {
        // Verifica o status da operação
        if (ac.ResultStatus == AccountResultStatus.Success)
        {
            return new OkObjectResult(ac);
        }

        return ac.ErrorCode switch
        {
            "error.invalid_account"             => new BadRequestObjectResult(new { errorCode = ac.ErrorCode, errorMessage = ac.ErrorMessage }),
            "error.account_already_exists"      => new BadRequestObjectResult(new { errorCode = ac.ErrorCode, errorMessage = ac.ErrorMessage }),
            "error.invalid_initial_balance"     => new BadRequestObjectResult(new { errorCode = ac.ErrorCode, errorMessage = ac.ErrorMessage }),
            "error.invalid_credit_limit"        => new BadRequestObjectResult(new { errorCode = ac.ErrorCode, errorMessage = ac.ErrorMessage }),
            "error.account_not_found"           => new NotFoundObjectResult(new { errorCode = ac.ErrorCode, errorMessage = ac.ErrorMessage }),
            "error.delete_not_allowed"          => new BadRequestObjectResult(new { errorCode = ac.ErrorCode, errorMessage = ac.ErrorMessage }),
            "error.unknown_error"               => new ObjectResult(new { errorCode = ac.ErrorCode, errorMessage = ac.ErrorMessage }) { StatusCode = 500 },
            _                                   => new ObjectResult(new { error = "error.unknown" }) { StatusCode = 500 }
        };
    }
}
