namespace TransactionProcessor.Models;

public enum TransactionStatus { Success, Failed, Pending }
public enum OperationType {Credit, Debit, Reserve, Capture, Reversal, Transfer}

public class TransactionRecord
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = Guid.NewGuid().ToString(); // id gerado
    public string ReferenceId { get; set; } = "";    // idempotency key (único)
    public string Operation { get; set; } = null;
    public string AccountIdentifier { get; set; } = null;
    public string? DestinationAccountIdentifier { get; set; } // para transferencia
    public long Amount { get; set; } // centavos
    public string Currency { get; set; } = "BRL";
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    // snapshot balances after processing
    public long BalanceAfter { get; set; }
    public long ReservedAfter { get; set; }
}
