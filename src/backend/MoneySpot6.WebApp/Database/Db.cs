using Microsoft.EntityFrameworkCore;

namespace MoneySpot6.WebApp.Database
{
    public class Db : DbContext
    {
        public DbSet<DbBankConnection> BankConnections { get; init; }
        public DbSet<DbBankAccount> BankAccounts { get; init; }
        public DbSet<DbBankAccountTransaction> BankAccountTransactions{ get; init; }
        public DbSet<DbStock> Stocks { get; init; }
        public DbSet<DbStockPrice> StockPrices { get; init; }

        public Db(DbContextOptions<Db> options) : base(options)
        {
        }
    }
}
