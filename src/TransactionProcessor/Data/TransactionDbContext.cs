using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Models;

namespace TransactionProcessor.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>()
            .ToTable("account")
            .HasIndex(a => a.AccountIdentifier)
            .IsUnique();

        modelBuilder.Entity<Account>()
            .ToTable("account")
            .Property(a => a.RowVersion)
            .IsRowVersion()
            .IsRequired();
    
        modelBuilder.Entity<TransactionRecord>()
            .ToTable("transaction")
            .HasIndex(t => t.ReferenceId)
            .IsUnique();
    }
}
