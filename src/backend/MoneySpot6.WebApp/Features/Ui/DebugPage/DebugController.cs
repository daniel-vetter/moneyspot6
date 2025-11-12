using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.AccountSync.Adapter;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing;

namespace MoneySpot6.WebApp.Features.Ui.DebugPage;

[ApiController]
[Route("api/[controller]")]
public class DebugController : Controller
{
    private readonly Db _db;
    private readonly TransactionProcessingFacade _transactionProcessingFacade;
    private readonly ExternalProcessMonitor _externalProcessMonitor;
    private readonly ILogger<DebugController> _logger;

    public DebugController(Db db, TransactionProcessingFacade transactionProcessingFacade, ExternalProcessMonitor externalProcessMonitor, ILogger<DebugController> logger)
    {
        _db = db;
        _transactionProcessingFacade = transactionProcessingFacade;
        _externalProcessMonitor = externalProcessMonitor;
        _logger = logger;
    }

    [HttpPost("ReprocessTransactionParsing")]
    public async Task ReprocessTransactionParsing()
    {
        _logger.LogInformation("Recalculating transactions...");
        var sw = Stopwatch.StartNew();
        await _transactionProcessingFacade.UpdateTransactions();
        await _db.SaveChangesAsync();
        _logger.LogInformation("Recalculated all transaction in {duration}.", sw.Elapsed);
    }

    [HttpPost("ReimportLast30DayStocks")]
    public Task ReimportLast30DayStocks()
    {
        //await _stockUpdater.Update(30, true, CancellationToken.None);
        return Task.CompletedTask;
    }

    [HttpGet("GetRunningAdapters")]
    public ImmutableArray<RunningProcessResponse> GetRunningAdapters()
    {
        return [
            .._externalProcessMonitor
                .GetRunningAdapters()
                .Select(x => new RunningProcessResponse(x.Id, x.StartTime, x.Error))
        ];
    }

    [HttpGet("GetAppDetails")]
    public AppDetails GetAppDetails()
    {
        return new AppDetails(
            Environment.GetEnvironmentVariable("BUILD_TIME") ?? "unknown", 
            Environment.GetEnvironmentVariable("BUILD_COMMIT") ?? "unknown", 
            Environment.Version.ToString(), 
            System.Runtime.InteropServices.RuntimeInformation.OSDescription
        );
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
            Name = "Lebensmittel"
        });
        _db.Categories.Add(new DbCategory
        {
            Name = "Miete"
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
        for (var i = 0; i < 1000; i++)
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
                Processed = new(),
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
                Note = "Seeded Transaction",
                IsNew = false
            });
        }
        
        _db.Rules.Add(new DbRule
        {
            Name = "Dummy Rule",
            OriginalCode = """
                           export function run(t: Transaction) {
                                if (t.purpose.endsWith("1")) {
                                    t.purpose = "Test";
                                }
                           }
                           """,
            CompiledCode = """
                           export function run(t) {
                                if (t.purpose.endsWith("1")) {
                                    t.purpose = "Test";
                                }
                           }
                           """,
            SourceMap = "",
            SortIndex = 1
        });

        var stock = new DbStock
        {
            Name = "iShares Core MSCI World ETF",
            Symbol = "EUNL.DE"
        };
        _db.Stocks.Add(stock);

        _db.StockTransactions.Add(new DbStockTransaction
        {
            Amount = 10,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Price = 100,
            Stock = stock
        });
        
        await _db.SaveChangesAsync();
    }
}

[PublicAPI]
public record AppDetails(string BuildTime, string BuildCommit, string DotNetVersion, string OSDescription);

[PublicAPI]
public record RunningProcessResponse(int ProcessId, DateTime? StartTime, string? Error);