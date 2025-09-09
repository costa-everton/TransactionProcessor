
using System.ComponentModel.DataAnnotations;

namespace TransactionProcessor.Models;

public enum TransactionStatus { Success, Failed, Pending }
public enum OperationType { Credit, Debit, Reserve, Capture, Reversal, Transfer }

public class TransactionRecord
{
    public int Id { get; set; }

    public string TransactionId { get; set; } = Guid.NewGuid().ToString(); // id gerado
    public string ReferenceId { get; set; } = "";    // idempotency key (único)

    public OperationType Operation { get; set; }
    public string AccountIdentifier { get; set; } = null!;
    public string? DestinationAccountIdentifier { get; set; }

    public long Amount { get; set; } // centavos
    public string Currency { get; set; } = "BRL";

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    // snapshots balances after processing
    public long BalanceAfter { get; set; }
    public long ReservedAfter { get; set; }

    // snapshots da conta de destino
    public long? DestinationBalanceAfter { get; set; }
    public long? DestinationReservedAfter { get; set; }
    public string? OriginalReferenceId { get; set; } // referência da transação original, usada em reversals


    // RowVersion para controle de concorrência
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Metadata opcional (ex: reversal_of, observações)
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
