using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TransactionProcessor.Models;

namespace TransactionProcessor.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<AccountRecord> Accounts => Set<AccountRecord>();
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração da Account
        modelBuilder.Entity<AccountRecord>()
            .ToTable("account")
            .HasIndex(a => a.AccountIdentifier)
            .IsUnique();

        modelBuilder.Entity<AccountRecord>()
            .Property(a => a.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired(false);

        // Configuração da Transaction
        modelBuilder.Entity<TransactionRecord>()
            .ToTable("transaction")
            .HasIndex(t => t.ReferenceId)
            .IsUnique();

        // Conversão do Metadata para JSON
        modelBuilder.Entity<TransactionRecord>()
            .Property(t => t.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
            );
        
        // RowVersion da Transaction para controle de concorrência
        modelBuilder.Entity<TransactionRecord>()
            .Property(t => t.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired(false);
    }
}
