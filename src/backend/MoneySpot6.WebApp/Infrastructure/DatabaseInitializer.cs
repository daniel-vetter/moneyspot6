using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Infrastructure;

[ScopedService]
public class DatabaseInitializer
{
    private readonly Db _db;

    public DatabaseInitializer(Db db)
    {
        _db = db;
    }

    public async Task Initialize()
    {
        await InitializeInflationDataAsync();
    }

    private async Task InitializeInflationDataAsync()
    {
        var settings = await _db.InflationSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new DbInflationSettings
            {
                DefaultRate = 1.9m
            };
            _db.InflationSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        var hasAnyData = await _db.InflationData.AnyAsync();
        if (!hasAnyData)
        {
            var defaultEntry = new DbInflationData
            {
                Year = 2020,
                Month = 1,
                IndexValue = 100m
            };
            _db.InflationData.Add(defaultEntry);
            await _db.SaveChangesAsync();
        }
    }
}
