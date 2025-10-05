using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync.Services;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;

namespace MoneySpot6.WebApp.Features.Debug;

[ApiController]
[Route("api/[controller]")]
public class DebugController : Controller
{
    private readonly Db _db;
    private readonly TransactionDetailsCalculator _transactionDetailsCalculator;
    private readonly ExternalProcessMonitor _externalProcessMonitor;
    private readonly ILogger<DebugController> _logger;

    public DebugController(Db db, TransactionDetailsCalculator transactionDetailsCalculator, ExternalProcessMonitor externalProcessMonitor, ILogger<DebugController> logger)
    {
        _db = db;
        _transactionDetailsCalculator = transactionDetailsCalculator;
        _externalProcessMonitor = externalProcessMonitor;
        _logger = logger;
    }

    [HttpPost("ReprocessTransactionParsing")]
    public async Task ReprocessTransactionParsing()
    {
        _logger.LogInformation("Recalculating transactions...");
        var sw = Stopwatch.StartNew();
        var transactions = await _db.BankAccountTransactions.AsTracking().ToArrayAsync();
        foreach (var transaction in transactions)
        {
            transaction.Parsed = _transactionDetailsCalculator.Parse(transaction.Raw);
            transaction.Final = _transactionDetailsCalculator.GetFinal(transaction.Parsed, transaction.Overridden);
        }
        await _db.SaveChangesAsync();

        _logger.LogInformation("Recalculated {count} transaction in {duration}.", transactions.Length, sw.Elapsed);
    }

    [HttpPost("ReimportLast30DayStocks")]
    public async Task ReimportLast30DayStocks()
    {
        //await _stockUpdater.Update(30, true, CancellationToken.None);
    }

    [HttpGet("GetRunningAdapters")]
    public async Task<ImmutableArray<RunningProcessResponse>> GetRunningAdapters()
    {
        return [
            .._externalProcessMonitor
                .GetRunningAdapters()
                .Select(x => new RunningProcessResponse(x.Id, x.StartTime, x.Error))
        ];
    }

    [HttpPost("ReseedDatabase")]
    public async Task ReseedDatabase()
    {
        await _db.Database.ExecuteSqlAsync($"DROP SCHEMA public CASCADE;");
        await _db.Database.ExecuteSqlAsync($"CREATE SCHEMA public;");
        await _db.Database.ExecuteSqlAsync($"GRANT ALL ON SCHEMA public TO postgres;");
        await _db.Database.ExecuteSqlAsync($"GRANT ALL ON SCHEMA public TO public;");
        await _db.Database.MigrateAsync();

        _db.Categories.Add(new DbCategory
        {
            Name = "Lebensmittel",
            AutoAssignmentCounterpartyRegex = "REWE|EDEKA|ALDI",
            AutoAssignmentPurposeRegex = "Einkauf|Supermarkt"
        });
        _db.Categories.Add(new DbCategory
        {
            Name = "Miete",
            AutoAssignmentCounterpartyRegex = "Vermieter|Wohnungsbau",
            AutoAssignmentPurposeRegex = "Miete|Wohnung"
        });
        await _db.SaveChangesAsync();

        var bankConnection = new DbBankConnection
        {
            Name = "TestBank",
            HbciVersion = "300",
            BankCode = "12345678",
            CustomerId = "CUST1",
            UserId = "USER1",
            Pin = "1234"
        };
        _db.BankConnections.Add(bankConnection);
        await _db.SaveChangesAsync();

        var bankAccount = new DbBankAccount
        {
            BankConnection = bankConnection,
            Icon = null,
            IconColor = null,
            Name = "TestKonto",
            Name2 = null,
            Country = "DE",
            Currency = "EUR",
            Bic = "TESTBIC",
            Iban = "DE12345678901234567890",
            BankCode = "12345678",
            AccountNumber = "1234567890",
            CustomerId = "CUST1",
            AccountType = "Giro",
            Type = "Giro",
            Balance = 10000m
        };
        _db.BankAccounts.Add(bankAccount);
        await _db.SaveChangesAsync();

        var random = new Random(42);
        for (int i = 0; i < 1000; i++)
        {
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(-i));
            var amount = Math.Round((decimal)(random.NextDouble() * 200 - 100), 2); // -100 bis +100
            _db.BankAccountTransactions.Add(new DbBankAccountTransaction
            {
                Source = "Seed",
                BankAccount = bankAccount,
                Raw = new DbBankAccountTransactionRawData
                {
                    Date = date,
                    Counterparty = new CounterpartyAccount { Name = $"Shop {random.Next(1, 10)}" },
                    Purpose = $"Testüberweisung {i}",
                    Amount = amount,
                    NewBalance = 10000 + amount,
                    IsCancelation = false
                },
                Parsed = new DbBankAccountTransactionParsedData
                {
                    Date = date,
                    Purpose = $"Testüberweisung {i}",
                    Name = $"Shop {random.Next(1, 10)}",
                    BankCode = "12345678",
                    AccountNumber = "1234567890",
                    Iban = "DE12345678901234567890",
                    Bic = "TESTBIC",
                    Amount = amount,
                    EndToEndReference = $"E2E{i}",
                    CustomerReference = $"CUSTREF{i}",
                    MandateReference = "",
                    CreditorIdentifier = "",
                    OriginatorIdentifier = "",
                    AlternateInitiator = "",
                    AlternateReceiver = "",
                    PaymentProcessor = PaymentProcessor.None
                },
                Overridden = new DbBankAccountTransactionOverrideData(),
                Final = new DbBankAccountTransactionFinalData
                {
                    Date = date,
                    Purpose = $"Testüberweisung {i}",
                    Name = $"Shop {random.Next(1, 10)}",
                    BankCode = "12345678",
                    AccountNumber = "1234567890",
                    Iban = "DE12345678901234567890",
                    Bic = "TESTBIC",
                    Amount = amount,
                    EndToEndReference = $"E2E{i}",
                    CustomerReference = $"CUSTREF{i}",
                    MandateReference = "",
                    CreditorIdentifier = "",
                    OriginatorIdentifier = "",
                    AlternateInitiator = "",
                    AlternateReceiver = "",
                    PaymentProcessor = PaymentProcessor.None
                },
                Note = "Seeded Transaction"
            });
        }
        await _db.SaveChangesAsync();
    }
}

public record RunningProcessResponse(int ProcessId, DateTime? StartTime, string? Error);