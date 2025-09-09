namespace TransactionProcessor.Models;

public enum TransactionErrorCode
{
    InvalidAccount,                   // error.invalid_account
    InsufficientFunds,                // error.insufficient_funds
    InsufficientAvailableForReserve,  // error.insufficient_available_for_reserve
    InsufficientReserved,             // error.insufficient_reserved
    DestinationAccountRequired,       // error.destination_account_required
    DuplicateReference,               // error.duplicate_reference
    UnknownOperation,                 // error.unknown_operation
    OriginalReferenceRequired,        // error.original_reference_required
    OriginalTransactionNotFound ,     // error.original_transaction_not_found
}

public static class TransactionErrorCodeExtensions
{
    public static string ToErrorString(this TransactionErrorCode code) =>
    code switch
    {
        TransactionErrorCode.InvalidAccount => "error.invalid_account",
        TransactionErrorCode.InsufficientFunds => "error.insufficient_funds",
        TransactionErrorCode.InsufficientAvailableForReserve => "error.insufficient_available_for_reserve",
        TransactionErrorCode.InsufficientReserved => "error.insufficient_reserved",
        TransactionErrorCode.DestinationAccountRequired => "error.destination_account_required",
        TransactionErrorCode.DuplicateReference => "error.duplicate_reference",
        TransactionErrorCode.UnknownOperation => "error.unknown_operation",
        TransactionErrorCode.OriginalReferenceRequired => "error.original_reference_required",
        TransactionErrorCode.OriginalTransactionNotFound => "error.original_transaction_not_found",
        _ => "error.unknown"
    };

}
