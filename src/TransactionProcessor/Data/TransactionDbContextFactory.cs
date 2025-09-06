using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TransactionProcessor.Data
{
    public class TransactionDbContextFactory : IDesignTimeDbContextFactory<TransactionDbContext>
    {
        public TransactionDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TransactionDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=transactiondb;Username=pg;Password=pg123");

            return new TransactionDbContext(optionsBuilder.Options);
        }
    }
}
