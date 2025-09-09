namespace TransactionProcessor.DTO;

public record TransactionCommandDto(
    string Operation,
    string AccountId,
    long Amount,
    string Currency,
    string ReferenceId,
    string? DestinationAccountId = null,
    string? OriginalReferenceId = null,
    Dictionary<string, string>? Metadata = null
);
