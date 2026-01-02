using MoneySpot6.WebApp.Features.Core.AccountSync.Adapter;
using MoneySpot6.WebApp.Features.Ui.InflationData.Import;
using MoneySpot6.WebApp.Features.Ui.Stocks.PriceImport.YahooAdapter;

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
        return services;
    }
}
