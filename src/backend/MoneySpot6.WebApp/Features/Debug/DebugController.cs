using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync.Services;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;
using MoneySpot6.WebApp.Features.Stocks.PriceImport;

namespace MoneySpot6.WebApp.Features.Debug
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : Controller
    {
        private readonly Db _db;
        private readonly RawDataParser _rawDataParser;
        private readonly ExternalProcessMonitor _externalProcessMonitor;
        private readonly StockUpdater _stockUpdater;

        public DebugController(Db db, RawDataParser rawDataParser, ExternalProcessMonitor externalProcessMonitor, StockUpdater stockUpdater)
        {
            _db = db;
            _rawDataParser = rawDataParser;
            _externalProcessMonitor = externalProcessMonitor;
            _stockUpdater = stockUpdater;
        }

        [HttpPost("ReprocessTransactionParsing")]
        public async Task ReprocessTransactionParsing()
        {
            var transactions = await _db.BankAccountTransactions.AsTracking().ToArrayAsync();
            foreach (var transaction in transactions)
            {
                transaction.Parsed = _rawDataParser.Parse(transaction.Raw);
            }
            await _db.SaveChangesAsync();
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
}
