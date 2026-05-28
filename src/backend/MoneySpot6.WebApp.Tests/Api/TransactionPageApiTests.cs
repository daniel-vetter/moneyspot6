using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.TransactionPage;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class TransactionPageApiTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    private async Task<DbBankAccount> CreateAccount()
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
        return account;
    }

    private async Task AddTransaction(DbBankAccount account, string finalName, string finalPurpose)
    {
        var db = Get<Db>();
        var date = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
        db.BankAccountTransactions.Add(new DbBankAccountTransaction
        {
            Source = "test",
            BankAccount = account,
            Note = "",
            IsNew = false,
            Raw = new DbBankAccountTransactionRawData
            {
                Date = date,
                Amount = -10m,
                Counterparty = new CounterpartyAccount()
            },
            Parsed = DbBankAccountTransactionParsedData.Default,
            Processed = new DbBankAccountTransactionProcessedData(),
            Overridden = new DbBankAccountTransactionOverrideData(),
            Final = new DbBankAccountTransactionFinalData
            {
                Date = date,
                Amount = -10m,
                Name = finalName,
                Purpose = finalPurpose,
                TransactionType = TransactionType.External
            }
        });
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task GetTransactions_WithSearch_DoesNotThrowAndMatchesName()
    {
        var account = await CreateAccount();
        await AddTransaction(account, finalName: "Amazon", finalPurpose: "Order 123");
        await AddTransaction(account, finalName: "Edeka", finalPurpose: "Groceries");

        // Regression: this previously threw on SQLite because EF.Functions.ILike is Postgres-only.
        var result = await Get<TransactionPageController>().GetTransactions("amazon", null, null, null);

        var response = result.ShouldBeOkObjectResult<TransactionResponse>();
        response.Entries.Length.ShouldBe(1);
        response.Entries[0].Name.ShouldBe("Amazon");
    }

    [Test]
    public async Task GetTransactions_SearchIsCaseInsensitive()
    {
        var account = await CreateAccount();
        await AddTransaction(account, finalName: "Amazon", finalPurpose: "Order 123");

        var lower = await Get<TransactionPageController>().GetTransactions("amazon", null, null, null);
        var upper = await Get<TransactionPageController>().GetTransactions("AMAZON", null, null, null);

        lower.ShouldBeOkObjectResult<TransactionResponse>().Entries.Length.ShouldBe(1);
        upper.ShouldBeOkObjectResult<TransactionResponse>().Entries.Length.ShouldBe(1);
    }

    [Test]
    public async Task GetTransactions_SearchMatchesPurpose()
    {
        var account = await CreateAccount();
        await AddTransaction(account, finalName: "Edeka", finalPurpose: "Monthly subscription");

        var result = await Get<TransactionPageController>().GetTransactions("subscription", null, null, null);

        result.ShouldBeOkObjectResult<TransactionResponse>().Entries.Length.ShouldBe(1);
    }

    [Test]
    public async Task GetTransactions_SearchNoMatch_ReturnsEmpty()
    {
        var account = await CreateAccount();
        await AddTransaction(account, finalName: "Amazon", finalPurpose: "Order 123");

        var result = await Get<TransactionPageController>().GetTransactions("nonexistent", null, null, null);

        result.ShouldBeOkObjectResult<TransactionResponse>().Entries.ShouldBeEmpty();
    }
}
