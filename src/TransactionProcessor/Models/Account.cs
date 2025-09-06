namespace TransactionProcessor.Models;
using System.ComponentModel.DataAnnotations;

public enum AccountStatus { Active, Inactive, Blocked }

public class Account
{
    public int Id { get; set; } 
    [Required]
    public string AccountIdentifier { get; set; } = null!; 
    public long Balance { get; set; }
    public long CreditLimit { get; set; }
    public long ReservedBalance { get; set; }   
    [Required]
    public AccountStatus Status { get; set; }  
    // RowVersion será gerada automaticamente pelo EF Core
    [Timestamp] 
    public byte[] RowVersion { get; set; } = null!;
}
