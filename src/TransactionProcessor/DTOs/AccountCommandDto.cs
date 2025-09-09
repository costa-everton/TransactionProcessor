namespace TransactionProcessor.DTO;

public record AccountCommandDto(
    string AccountId,
    string AccountHolderName,
    long InitialBalance = 0,
    long CreditLimit = 0
);
