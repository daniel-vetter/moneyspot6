using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;

namespace MoneySpot6.WebApp.Features.Shared
{
    [ScopedService]
    public class BalanceProvider
    {
        private readonly Db _db;
        private readonly StockDataProvider _stockDataProvider;

        public BalanceProvider(Db db, StockDataProvider stockDataProvider)
        {
            _db = db;
            _stockDataProvider = stockDataProvider;
        }

        public async Task<long> GetCurrentBalance(ImmutableArray<int>? accountIds = null)
        {
            if (accountIds.HasValue == false)
                return await _db.BankAccounts.Select(x => x.Balance).SumAsync();
            return await _db.BankAccounts.Where(x => accountIds.Value.Contains(x.Id)).Select(x => x.Balance).SumAsync();
        }

        public async Task<long> GetBalanceAtStartOf(DateOnly date, ImmutableArray<int>? accountIds = null)
        {
            if (accountIds == null)
                accountIds = [.. await _db.BankAccounts.Select(x => x.Id).ToArrayAsync()];

            long sum = 0;
            foreach (var accountId in accountIds)
            {
                var balance = await _db.BankAccountTransactions
                    .OrderByDescending(x => x.Raw.Date)
                    .ThenByDescending(x => x.Id)
                    .Where(x => x.BankAccount.Id == accountId)
                    .Where(x => x.Raw.Date < date)
                    .Select(x => x.Raw.NewBalance)
                    .FirstOrDefaultAsync();

                sum += balance;
            }
            return sum;
        }

        public async Task<ImmutableArray<BalanceHistoryEntry>> GetBalanceHistory(DateOnly startDate, DateOnly endDate, ImmutableArray<int>? accountIds = null)
        {
            var resultList = new List<ChangeableBalanceHistoryEntry>();
            for (var cur = startDate; cur < endDate; cur = cur.AddDays(1))
                resultList.Add(new ChangeableBalanceHistoryEntry(cur));

            var resultIndex = resultList.ToDictionary(x => x.Date, x => x);

            if (accountIds == null)
                accountIds = [..await _db.BankAccounts.Select(x => x.Id).ToArrayAsync()];

            // Go through each requested account and add the balance
            foreach (var accountId in accountIds)
            {
                // Find the start balance
                var startBalance = await _db.BankAccountTransactions
                    .Where(x => x.BankAccount.Id == accountId && x.Raw.Date < startDate)
                    .OrderByDescending(x => x.Raw.Date)
                    .ThenByDescending(x => x.Id)
                    .Select(x => (long?)x.Raw.NewBalance)
                    .FirstOrDefaultAsync();

                // Find all balance changes
                var balanceChanges = (await _db.BankAccountTransactions
                        .Where(x => x.BankAccount.Id == accountId && x.Raw.Date >= startDate && x.Raw.Date < endDate)
                        .OrderBy(x => x.Raw.Date)
                        .ThenBy(x => x.Id)
                        .Select(x => new
                        {
                            x.Raw.Date,
                            x.Raw.NewBalance
                        })
                        .ToArrayAsync())
                    .GroupBy(x => x.Date)
                    .ToDictionary(x => x.Key, x => x.Last().NewBalance);

                // Apply the values
                var balance = startBalance ?? 0L;
                for (var cur = startDate; cur < endDate; cur = cur.AddDays(1))
                {
                    if (balanceChanges.TryGetValue(cur, out var balanceOfThisDay))
                        balance = balanceOfThisDay;

                    resultIndex[cur].Balance += balance;
                }
            }

            return [..resultList.Select(x => new BalanceHistoryEntry(x.Date, x.Balance))];
        }

        private record ChangeableBalanceHistoryEntry(DateOnly Date)
        {
            public long Balance { get; set; }
        }
    }

    public record BalanceHistoryEntry(DateOnly Date, long Balance);
}
