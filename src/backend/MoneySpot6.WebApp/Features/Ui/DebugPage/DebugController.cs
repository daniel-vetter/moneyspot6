using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing;
using MoneySpot6.WebApp.Infrastructure;

namespace MoneySpot6.WebApp.Features.Ui.DebugPage;

[ApiController]
[Route("api/[controller]")]
public class DebugController : Controller
{
    private readonly Db _db;
    private readonly TransactionProcessingFacade _transactionProcessingFacade;
    private readonly ILogger<DebugController> _logger;
    private readonly DatabaseInitializer _databaseInitializer;

    public DebugController(Db db, TransactionProcessingFacade transactionProcessingFacade, ILogger<DebugController> logger, DatabaseInitializer databaseInitializer)
    {
        _db = db;
        _transactionProcessingFacade = transactionProcessingFacade;
        _logger = logger;
        _databaseInitializer = databaseInitializer;
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

[HttpGet("GetAppDetails")]
    public AppDetails GetAppDetails()
    {
        var databaseType = _db switch
        {
            PostgreSqlDbContext => "PostgreSQL",
            SqliteDbContext => "SQLite",
            _ => "Unknown"
        };

        return new AppDetails(
            Environment.GetEnvironmentVariable("BUILD_TIME") ?? "unknown",
            Environment.GetEnvironmentVariable("BUILD_COMMIT") ?? "unknown",
            Environment.Version.ToString(),
            System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            databaseType
        );
    }

    [HttpPost("ReseedDatabase")]
    public async Task ReseedDatabase()
    {
        if (_db is PostgreSqlDbContext)
        {
            await _db.Database.ExecuteSqlAsync($"DROP SCHEMA public CASCADE;");
            await _db.Database.ExecuteSqlAsync($"CREATE SCHEMA public;");
            await _db.Database.ExecuteSqlAsync($"GRANT ALL ON SCHEMA public TO postgres;");
            await _db.Database.ExecuteSqlAsync($"GRANT ALL ON SCHEMA public TO public;");
        }
        if (_db is SqliteDbContext)
        {
            await _db.Database.EnsureDeletedAsync();
        }
        
        await _db.Database.MigrateAsync();
        await _databaseInitializer.Initialize(true);



    }
}

[PublicAPI]
public record AppDetails(string BuildTime, string BuildCommit, string DotNetVersion, string OSDescription, string DatabaseType);
