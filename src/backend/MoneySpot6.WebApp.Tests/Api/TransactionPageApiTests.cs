using System.Text;
using Microsoft.AspNetCore.Mvc;
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

    private async Task AddTransaction(DbBankAccount account, string finalName, string finalPurpose, decimal amount = -10m)
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
                Amount = amount,
                Counterparty = new CounterpartyAccount()
            },
            Parsed = DbBankAccountTransactionParsedData.Default,
            Processed = new DbBankAccountTransactionProcessedData(),
            Overridden = new DbBankAccountTransactionOverrideData(),
            Final = new DbBankAccountTransactionFinalData
            {
                Date = date,
                Amount = amount,
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

    private async Task<string[]> ExportCsvLines(bool includeRaw, bool includeFinal)
    {
        var result = await Get<TransactionPageController>().Export(includeRaw, includeFinal);

        var file = result.ShouldBeOfType<FileContentResult>();
        file.ContentType.ShouldBe("text/csv");

        var text = Encoding.UTF8.GetString(file.FileContents);
        text.ShouldStartWith("\uFEFF"); // UTF-8 BOM so Excel detects the encoding
        return text.TrimStart('\uFEFF').Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    [Test]
    public async Task Export_FinalOnly_ContainsFinalColumnsButNoRawColumns()
    {
        var account = await CreateAccount();
        await AddTransaction(account, finalName: "Amazon", finalPurpose: "Order 123");

        var lines = await ExportCsvLines(includeRaw: false, includeFinal: true);

        lines.Length.ShouldBe(2);
        lines[0].ShouldStartWith("Id;Account;Currency;Source;Final.Date;Final.Name;Final.Purpose;Final.Amount");
        lines[0].ShouldNotContain("Raw.");
        lines[1].ShouldContain("Test Account;EUR;test;");
        lines[1].ShouldContain(";Amazon;Order 123;-10");
    }

    [Test]
    public async Task Export_RawOnly_ContainsRawColumnsButNoFinalColumns()
    {
        var account = await CreateAccount();
        await AddTransaction(account, finalName: "Amazon", finalPurpose: "Order 123");

        var lines = await ExportCsvLines(includeRaw: true, includeFinal: false);

        lines[0].ShouldContain("Raw.Date;Raw.Amount;Raw.Purpose");
        lines[0].ShouldNotContain("Final.");
        lines[1].ShouldNotContain("Amazon");
    }

    [Test]
    public async Task Export_Both_ContainsBothColumnGroups()
    {
        var account = await CreateAccount();
        await AddTransaction(account, finalName: "Amazon", finalPurpose: "Order 123");

        var lines = await ExportCsvLines(includeRaw: true, includeFinal: true);

        lines[0].ShouldContain("Final.Date");
        lines[0].ShouldContain("Raw.Date");
    }

    [Test]
    public async Task Export_ValuesWithSeparatorAndQuotes_AreEscaped()
    {
        var account = await CreateAccount();
        await AddTransaction(account, finalName: "Some;Name", finalPurpose: "He said \"hi\"");

        var lines = await ExportCsvLines(includeRaw: false, includeFinal: true);

        lines[1].ShouldContain("\"Some;Name\"");
        lines[1].ShouldContain("\"He said \"\"hi\"\"\"");
    }

    [Test]
    public async Task Export_DecimalAmounts_UseGermanFormat()
    {
        var account = await CreateAccount();
        await AddTransaction(account, finalName: "Amazon", finalPurpose: "Order 123", amount: -10.50m);

        var lines = await ExportCsvLines(includeRaw: false, includeFinal: true);

        // SQLite loses the trailing zero on the roundtrip (-10,5), Postgres keeps it (-10,50) —
        // only the decimal separator matters here.
        lines[1].ShouldContain(";-10,5");
    }

    [Test]
    public async Task Export_NothingSelected_ReturnsBadRequest()
    {
        var result = await Get<TransactionPageController>().Export(includeRaw: false, includeFinal: false);

        result.ShouldBeOfType<BadRequestResult>();
    }
}
