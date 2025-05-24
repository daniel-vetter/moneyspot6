using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Common;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Shared;

[ScopedService]
public class BalanceProvider
{
    private readonly Db _db;

    public BalanceProvider(Db db)
    {
        _db = db;
    }

    public async Task<decimal> GetCurrentBalance(ImmutableArray<int>? accountIds = null)
    {
        if (accountIds.HasValue == false)
            return await _db.BankAccounts.Select(x => x.Balance).SumAsync();
        return await _db.BankAccounts.Where(x => accountIds.Value.Contains(x.Id)).Select(x => x.Balance).SumAsync();
    }

    public async Task<decimal> GetBalanceAtStartOf(DateOnly date, ImmutableArray<int>? accountIds = null)
    {
        if (accountIds == null)
            accountIds = [.. await _db.BankAccounts.Select(x => x.Id).ToArrayAsync()];

        decimal sum = 0;
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

    public async Task<ImmutableTimeline<decimal>> GetBalanceHistory(DateOnly startDate, DateOnly endDate, ImmutableArray<int>? accountIds = null)
    {
        accountIds ??= [..await _db.BankAccounts.Select(x => x.Id).ToArrayAsync()];
        var balances = new decimal[endDate.DayNumber - startDate.DayNumber];

        // Go through each requested account and add the balance
        foreach (var accountId in accountIds)
        {
            // Find the start balance
            var startBalance = await _db.BankAccountTransactions
                .Where(x => x.BankAccount.Id == accountId && x.Raw.Date < startDate)
                .OrderByDescending(x => x.Raw.Date)
                .ThenByDescending(x => x.Id)
                .Select(x => (decimal?)x.Raw.NewBalance)
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
            var balance = startBalance ?? 0m;
            for (var cur = startDate; cur < endDate; cur = cur.AddDays(1))
            {
                if (balanceChanges.TryGetValue(cur, out var balanceOfThisDay))
                    balance = balanceOfThisDay;

                balances[cur.DayNumber - startDate.DayNumber] += balance;
            }
        }

        return ImmutableTimeline.Create(startDate, endDate, balances);
    }
}