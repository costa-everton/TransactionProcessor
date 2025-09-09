namespace TransactionProcessor.Models;

public enum AccountErrorCode
{
    InvalidAccount,           // error.invalid_account
    AccountAlreadyExists,     // error.account_already_exists
    InvalidInitialBalance,    // error.invalid_initial_balance
    InvalidCreditLimit,       // error.invalid_credit_limit
    AccountNotFound,          // error.account_not_found
    DeleteNotAllowed,         // error.delete_not_allowed
    UnknownError              // error.unknown_error
}

public static class AccountErrorCodeExtensions
{
    public static string ToErrorString(this AccountErrorCode code) =>
        code switch
        {
            AccountErrorCode.InvalidAccount        => "error.invalid_account",
            AccountErrorCode.AccountAlreadyExists  => "error.account_already_exists",
            AccountErrorCode.InvalidInitialBalance => "error.invalid_initial_balance",
            AccountErrorCode.InvalidCreditLimit    => "error.invalid_credit_limit",
            AccountErrorCode.AccountNotFound       => "error.account_not_found",
            AccountErrorCode.DeleteNotAllowed      => "error.delete_not_allowed",
            AccountErrorCode.UnknownError          => "error.unknown_error",
            _                                      => "error.unknown"
        };
}
    