using System.ComponentModel.DataAnnotations;

public enum AccountResultStatus { Success, Failed, Pending }
public enum AccountStatus { Active, Inactive, Blocked }

public class AccountRecord
{
    public int Id { get; set; }

    [Required]
    public string AccountIdentifier { get; set; } = null!;
    public string HolderName { get; set; } = null!;
    public long Balance { get; set; }
    public long CreditLimit { get; set; }
    public long ReservedBalance { get; set; }

    // Mantemos o status da conta
    public AccountStatus Status { get; set; } = AccountStatus.Active;

    // Novo campo: resultado da operação (Success, Failed, Pending)
    public AccountResultStatus ResultStatus { get; set; } = AccountResultStatus.Pending;

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
