using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync.Services;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;

namespace MoneySpot6.WebApp.Features.Debug
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : Controller
    {
        private readonly Db _db;
        private readonly RawDataParser _rawDataParser;
        private readonly ExternalProcessMonitor _externalProcessMonitor;

        public DebugController(Db db, RawDataParser rawDataParser, ExternalProcessMonitor externalProcessMonitor)
        {
            _db = db;
            _rawDataParser = rawDataParser;
            _externalProcessMonitor = externalProcessMonitor;
        }

        [HttpPost]
        public async Task ReprocessTransactionParsing()
        {
            var transactions = await _db.BankAccountTransactions.AsTracking().ToArrayAsync();
            foreach (var transaction in transactions)
            {
                transaction.Parsed = _rawDataParser.Parse(transaction.Raw);
            }
            await _db.SaveChangesAsync();
        }

        public async Task<ImmutableArray<RunningProcessResponse>> GetRunningProcesses()
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
