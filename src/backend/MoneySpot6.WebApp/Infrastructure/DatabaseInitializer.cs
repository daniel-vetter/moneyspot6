using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Infrastructure;

[ScopedService]
public class DatabaseInitializer(Db db)
{
    public async Task Initialize()
    {
        await InitializeInflationData();
    }

    private async Task InitializeInflationData()
    {
        var hasAnyData = await db.InflationData.AnyAsync();
        if (!hasAnyData)
        {
            var defaultEntry = new DbInflationData
            {
                Year = 2020,
                Month = 1,
                IndexValue = 100m
            };
            db.InflationData.Add(defaultEntry);
            await db.SaveChangesAsync();
        }
    }
}
