using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing;
using MoneySpot6.WebApp.Tests.Api;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.TransactionProcessing;

public class RuleOrderingTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    // A rule that unconditionally overwrites the purpose with a fixed value.
    private static string SetPurposeRule(string value) => $$"""
        export function run(t) {
            t.purpose = '{{value}}';
        }
        """;

    [Test]
    public async Task UpdateTransactions_AfterReorder_LastRuleBySortIndexWins()
    {
        var txId = await CreateTransaction();

        // Rule A is created first (lower Id), Rule B second (higher Id).
        // Both write the same field, so the rule that runs LAST wins.
        var ruleAId = await AddRule("Rule A", SetPurposeRule("A"));
        var ruleBId = await AddRule("Rule B", SetPurposeRule("B"));

        // Initially SortIndex order == creation order, so B runs last and wins.
        SwitchToNewScope();
        await Get<TransactionProcessingFacade>().UpdateTransactions();

        SwitchToNewScope();
        var afterInitial = await Get<Db>().BankAccountTransactions.FirstAsync(x => x.Id == txId);
        afterInitial.Final.Purpose.ShouldBe("B");

        // Reorder so that Rule A is now last in SortIndex order.
        // ReorderRules reprocesses all transactions internally.
        SwitchToNewScope();
        var reorder = await Get<TransactionProcessingFacade>()
            .ReorderRules(ImmutableArray.Create(ruleBId, ruleAId));
        reorder.Error.ShouldBeNull();

        // Rule A now runs last, so it must win.
        SwitchToNewScope();
        var afterReorder = await Get<Db>().BankAccountTransactions.FirstAsync(x => x.Id == txId);
        afterReorder.Final.Purpose.ShouldBe("A");
    }

    private async Task<int> AddRule(string name, string compiledCode)
    {
        var db = Get<Db>();
        var maxSortIndex = await db.Rules.AnyAsync()
            ? await db.Rules.MaxAsync(x => x.SortIndex)
            : 0;

        var rule = new DbRule
        {
            Name = name,
            OriginalCode = "// " + name,
            CompiledCode = compiledCode,
            SourceMap = "",
            SortIndex = maxSortIndex + 1
        };
        db.Rules.Add(rule);
        await db.SaveChangesAsync();
        return rule.Id;
    }

    private async Task<int> CreateTransaction()
    {
        var db = Get<Db>();
        var connection = new DbBankConnection { Name = "Test", Type = BankConnectionType.Demo, Settings = "{}" };
        db.BankConnections.Add(connection);
        var account = new DbBankAccount
        {
            BankConnection = connection,
            Name = "Acc",
            Name2 = null,
            Country = "DE",
            Currency = "EUR",
            Bic = "TEST",
            Iban = "DE00TEST",
            BankCode = "12345",
            AccountNumber = "12345",
            CustomerId = "1",
            AccountType = "Checking",
            Type = "Checking",
            Balance = 0
        };
        db.BankAccounts.Add(account);
        await db.SaveChangesAsync();

        var date = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
        var tx = new DbBankAccountTransaction
        {
            Source = "test",
            BankAccount = account,
            Note = "",
            IsNew = false,
            Raw = new DbBankAccountTransactionRawData
            {
                Date = date,
                Amount = -10m,
                Purpose = "original",
                Counterparty = new CounterpartyAccount()
            },
            Parsed = DbBankAccountTransactionParsedData.Default,
            Processed = new DbBankAccountTransactionProcessedData(),
            Overridden = new DbBankAccountTransactionOverrideData(),
            Final = new DbBankAccountTransactionFinalData { Date = date, Amount = -10m, TransactionType = TransactionType.External }
        };
        db.BankAccountTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx.Id;
    }
}
