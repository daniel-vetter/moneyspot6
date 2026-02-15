using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.AccountSync.FinTs.Adapter;
using MoneySpot6.WebApp.Features.Ui.InflationData.Import;
using MoneySpot6.WebApp.Features.Ui.Stocks.PriceImport.YahooAdapter;
using System.Data.Common;

namespace MoneySpot6.WebApp;

public static class Module
{
    public static IServiceCollection RegisterAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceFromAttributes();
        services.AddHttpClient<GenesisApiClient>();
        services.AddHttpClient<YahooStockDataClient>(x =>
        {
            x.DefaultRequestHeaders.Host = "query1.finance.yahoo.com";
            x.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
        });
        services.Configure<MailIntegrationOptions>(configuration.GetSection("MailIntegration"));
        services.Configure<InflationImportOptions>(configuration.GetSection("InflationImport"));
        services.Configure<HbciAdapterOptions>(configuration.GetSection("HbciAdapter"));
        services.AddDb(configuration);
        return services;
    }

    private static IServiceCollection AddDb(this IServiceCollection services, IConfiguration configuration)
    {
        var conStr = configuration.GetConnectionString("db");
        if (string.IsNullOrEmpty(conStr))
        {
            conStr = "Data Source=" + Path.Combine(AppContext.BaseDirectory, "data", "data.db");
        }

        var conStrBuilder = new DbConnectionStringBuilder { ConnectionString = conStr };
        if (conStrBuilder.ContainsKey("Data Source"))
        {
            var dataSource = conStrBuilder["Data Source"]?.ToString() ?? throw new Exception("Could not extract Data Source from connection string");
            var dirName = Path.GetDirectoryName(dataSource) ?? throw new Exception("Could not extract directory from ");
            if (!string.IsNullOrWhiteSpace(dirName) && !Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            services.AddDbContext<Db, SqliteDbContext>(x =>
            {
                x.UseSqlite(conStr);
            });
        }
        else
        {
            services.AddDbContext<Db, PostgreSqlDbContext>(x =>
            {
                x.UseNpgsql(conStr);
            });
        }

        return services;
    }
}
