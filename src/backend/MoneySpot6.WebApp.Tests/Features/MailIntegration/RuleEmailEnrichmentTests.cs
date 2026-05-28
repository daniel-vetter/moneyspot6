using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing;
using MoneySpot6.WebApp.Tests.Api;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.MailIntegration;

public class RuleEmailEnrichmentTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    // A rule that attaches data from a matching processed email onto the transaction.
    private const string FindMailRuleCode = """
        export function run(t) {
            const mail = findMail({ merchant: 'Amazon' });
            if (mail) {
                t.purpose = 'MATCHED:' + mail.Merchant;
            }
        }
        """;

    private async Task<DbBankAccountTransaction> CreateTransaction()
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
        return tx;
    }

    private async Task AddRule()
    {
        Get<Db>().Rules.Add(new DbRule
        {
            Name = "FindMail Rule",
            OriginalCode = "// find mail",
            CompiledCode = FindMailRuleCode,
            SourceMap = "",
            SortIndex = 1
        });
        await Get<Db>().SaveChangesAsync();
    }

    private async Task AddProcessedEmail(string merchant)
    {
        var db = Get<Db>();
        var account = new DbGMailIntegration { Name = "a@example.com", AccessToken = "x", RefreshToken = "x", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };
        var monitored = new DbMonitoredEmailAddress { EmailAddress = "shop@example.com" };
        db.Add(account);
        db.Add(monitored);
        await db.SaveChangesAsync();

        db.Add(new DbImportedEmail
        {
            GMailAccount = account,
            MonitoredAddress = monitored,
            MessageId = "msg-1",
            InternalDate = DateTimeOffset.UtcNow.AddHours(-1),
            FromAddress = "shop@example.com",
            Subject = "Order",
            Body = "...",
            ImportedAt = DateTimeOffset.UtcNow,
            ProcessedAt = DateTimeOffset.UtcNow,
            ProcessedData = new DbExtractedEmailData { Merchant = merchant }
        });
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task UpdateTransactions_WithMatchingProcessedEmail_EnrichesTransaction()
    {
        var tx = await CreateTransaction();
        await AddRule();
        await AddProcessedEmail("Amazon");

        await Get<TransactionProcessingFacade>().UpdateTransactions();

        var reloaded = await ReloadTransaction(tx.Id);
        reloaded.Final.Purpose.ShouldBe("MATCHED:Amazon");
    }

    [Test]
    public async Task UpdateTransactions_WithoutProcessedEmail_LeavesTransactionUnenriched()
    {
        var tx = await CreateTransaction();
        await AddRule();
        // No processed email -> findMail returns null -> rule does not set purpose.

        await Get<TransactionProcessingFacade>().UpdateTransactions();

        var reloaded = await ReloadTransaction(tx.Id);
        reloaded.Final.Purpose.ShouldNotBe("MATCHED:Amazon");
    }

    [Test]
    public async Task UpdateTransactions_EmailMerchantDoesNotMatchFilter_LeavesTransactionUnenriched()
    {
        var tx = await CreateTransaction();
        await AddRule();
        await AddProcessedEmail("Edeka"); // rule filters for 'Amazon'

        await Get<TransactionProcessingFacade>().UpdateTransactions();

        var reloaded = await ReloadTransaction(tx.Id);
        reloaded.Final.Purpose.ShouldNotBe("MATCHED:Amazon");
    }

    private async Task<DbBankAccountTransaction> ReloadTransaction(int id)
    {
        Get<Db>().ChangeTracker.Clear();
        return await Get<Db>().BankAccountTransactions.AsNoTracking().FirstAsync(x => x.Id == id);
    }
}
