using System.Collections.Immutable;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.HistoryPage;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class AccountHistoryApiTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    [Test]
    public async Task Get_EmptyDatabase_ReturnsEmptyArray()
    {
        var result = await Get<AccountHistoryController>().Get();

        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task Get_OnlyStockTransactions_ReturnsHistoryFromStockDate()
    {
        var db = Get<Db>();
        var stock = new DbStock { Name = "Test", Symbol = "TEST" };
        db.Stocks.Add(stock);
        await db.SaveChangesAsync();

        var stockDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-3);
        db.StockTransactions.Add(new DbStockTransaction
        {
            Stock = stock,
            Date = stockDate,
            Amount = 5,
            Price = 100
        });
        await db.SaveChangesAsync();

        var result = await Get<AccountHistoryController>().Get();

        var history = result.Value;
        history.Length.ShouldBeGreaterThan(0);
        history[0].Date.ShouldBe(stockDate);
    }

    [Test]
    public async Task Get_OnlyBankTransactions_ReturnsHistoryFromTransactionDate()
    {
        var db = Get<Db>();
        var connection = new DbBankConnection { Name = "Test", Type = BankConnectionType.Demo, Settings = "{}" };
        db.BankConnections.Add(connection);
        await db.SaveChangesAsync();

        var account = new DbBankAccount
        {
            BankConnection = connection,
            Name = "Test Account",
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

        var transactionDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-3);
        db.BankAccountTransactions.Add(new DbBankAccountTransaction
        {
            Source = "test",
            BankAccount = account,
            Note = "",
            IsNew = false,
            Raw = new DbBankAccountTransactionRawData
            {
                Date = transactionDate,
                Amount = -50m,
                Counterparty = new CounterpartyAccount()
            },
            Parsed = new DbBankAccountTransactionParsedData
            {
                Date = transactionDate,
                Amount = -50m,
                Purpose = "",
                Name = "Test",
                BankCode = "",
                AccountNumber = "",
                Iban = "",
                Bic = "",
                EndToEndReference = "",
                CustomerReference = "",
                MandateReference = "",
                CreditorIdentifier = "",
                OriginatorIdentifier = "",
                AlternateInitiator = "",
                AlternateReceiver = "",
                PaymentProcessor = PaymentProcessor.None,
                TransactionType = TransactionType.External
            },
            Processed = new DbBankAccountTransactionProcessedData(),
            Overridden = new DbBankAccountTransactionOverrideData(),
            Final = new DbBankAccountTransactionFinalData
            {
                Date = transactionDate,
                Amount = -50m,
                TransactionType = TransactionType.External
            }
        });
        await db.SaveChangesAsync();

        var result = await Get<AccountHistoryController>().Get();

        var history = result.Value;
        history.Length.ShouldBeGreaterThan(0);
        history[0].Date.ShouldBe(transactionDate);
    }
}
