using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Ui.InflationData.Import;

[ScopedService]
public class VpiUpdater
{
    private readonly GenesisApiClient _genesisApiClient;
    private readonly Db _db;
    private readonly ILogger<VpiUpdater> _logger;
    private readonly IOptions<InflationImportOptions> _options;

    public VpiUpdater(GenesisApiClient genesisApiClient, Db db, ILogger<VpiUpdater> logger, IOptions<InflationImportOptions> options)
    {
        _genesisApiClient = genesisApiClient;
        _db = db;
        _logger = logger;
        _options = options;
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Value.GenesisApiToken))
        {
            _logger.LogInformation("Skipped VPI data update because no api token is configured.");
            return;
        }

        _logger.LogInformation("Starting VPI data update");

        var dataPoints = await _genesisApiClient.GetVpiData(cancellationToken);

        var existingData = await _db
            .InflationData
            .ToDictionaryAsync(x => (x.Year, x.Month), x => x, cancellationToken);

        var imported = 0;
        var updated = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var dataPoint in dataPoints)
        {
            if (!existingData.TryGetValue((dataPoint.Year, dataPoint.Month), out var existing))
            {
                _db.InflationData.Add(new DbInflationData
                {
                    Year = dataPoint.Year,
                    Month = dataPoint.Month,
                    IndexValue = dataPoint.Value,
                    ImportedAt = now
                });
                imported++;
            }
            else if (existing.IndexValue != dataPoint.Value)
            {
                existing.IndexValue = dataPoint.Value;
                existing.ImportedAt = now;
                updated++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("VPI data update completed: {Imported} imported, {Updated} updated", imported, updated);
    }
}
