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
}

public record RunningProcessResponse(int ProcessId, DateTime? StartTime, string? Error);