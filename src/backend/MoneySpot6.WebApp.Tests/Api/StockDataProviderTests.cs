using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.Shared;
using MoneySpot6.WebApp.Features.Ui.SummaryPage;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class StockDataProviderTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    private async Task<(DbStock stockA, DbStock stockB)> SetupTwoStocks()
    {
        var db = Get<Db>();

        var stockA = new DbStock { Name = "Stock A", Symbol = "STOCKA" };
        var stockB = new DbStock { Name = "Stock B", Symbol = "STOCKB" };
        db.Stocks.Add(stockA);
        db.Stocks.Add(stockB);
        await db.SaveChangesAsync();

        // Stock A: 10 shares bought on 2024-01-15 at $100/share
        db.StockTransactions.Add(new DbStockTransaction
        {
            Stock = stockA,
            Date = new DateOnly(2024, 1, 15),
            Amount = 10,
            Price = 100
        });

        // Stock B: 5 shares bought on 2024-01-20 at $200/share
        db.StockTransactions.Add(new DbStockTransaction
        {
            Stock = stockB,
            Date = new DateOnly(2024, 1, 20),
            Amount = 5,
            Price = 200
        });

        // Daily prices for Stock A: Open=105, Close=110
        db.StockPrices.Add(new DbStockPrice
        {
            Stock = stockA,
            Timestamp = new DateTimeOffset(2024, 1, 25, 0, 0, 0, TimeSpan.Zero),
            Interval = StockPriceInterval.Daily,
            Open = 105,
            Close = 110,
            High = 112,
            Low = 104,
            Volume = 1000
        });

        // Daily prices for Stock B: Open=210, Close=220
        db.StockPrices.Add(new DbStockPrice
        {
            Stock = stockB,
            Timestamp = new DateTimeOffset(2024, 1, 25, 0, 0, 0, TimeSpan.Zero),
            Interval = StockPriceInterval.Daily,
            Open = 210,
            Close = 220,
            High = 225,
            Low = 208,
            Volume = 500
        });

        // FiveMinutes prices with deliberately different close values
        // to verify Dashboard and Cashflow use the same price source (Daily)
        db.StockPrices.Add(new DbStockPrice
        {
            Stock = stockA,
            Timestamp = new DateTimeOffset(2024, 1, 25, 15, 0, 0, TimeSpan.Zero),
            Interval = StockPriceInterval.FiveMinutes,
            Open = 109,
            Close = 115,
            High = 116,
            Low = 109,
            Volume = 100
        });

        db.StockPrices.Add(new DbStockPrice
        {
            Stock = stockB,
            Timestamp = new DateTimeOffset(2024, 1, 25, 15, 0, 0, TimeSpan.Zero),
            Interval = StockPriceInterval.FiveMinutes,
            Open = 219,
            Close = 230,
            High = 231,
            Low = 218,
            Volume = 50
        });

        await db.SaveChangesAsync();
        return (stockA, stockB);
    }

    [Test]
    public async Task GetDailyOwnedStockValue_MultipleStocks_AccumulatesValues()
    {
        var (stockA, stockB) = await SetupTwoStocks();

        var provider = Get<StockDataProvider>();
        var start = new DateOnly(2024, 1, 25);
        var end = new DateOnly(2024, 1, 26);

        var result = await provider.GetDailyOwnedStockValue(start, end);

        var day = result[start];

        // Stock A: 10 shares * $105 open = $1050, 10 * $110 close = $1100
        // Stock B: 5 shares * $210 open = $1050, 5 * $220 close = $1100
        // Total: open = $2100, close = $2200
        day.EndOfDay.CurrentValue.ShouldBe(2200m);
        day.StartOfDay.CurrentValue.ShouldBe(2100m);

        // Invested: Stock A = 10 * $100 = $1000, Stock B = 5 * $200 = $1000
        // Total invested = $2000
        day.StartOfDay.InvestedValue.ShouldBe(2000m);
        day.EndOfDay.InvestedValue.ShouldBe(2000m);
    }

    [Test]
    public async Task GetDailyOwnedStockValue_SingleStock_ReturnsCorrectValue()
    {
        var (stockA, _) = await SetupTwoStocks();

        var provider = Get<StockDataProvider>();
        var start = new DateOnly(2024, 1, 25);
        var end = new DateOnly(2024, 1, 26);

        var result = await provider.GetDailyOwnedStockValue(start, end, [stockA.Id]);

        var day = result[start];

        // Only Stock A: 10 shares * $110 close = $1100
        day.EndOfDay.CurrentValue.ShouldBe(1100m);
        day.StartOfDay.CurrentValue.ShouldBe(1050m);
        day.StartOfDay.InvestedValue.ShouldBe(1000m);
        day.EndOfDay.InvestedValue.ShouldBe(1000m);
    }

    [Test]
    public async Task CashflowStockBalance_MatchesDashboardStockTotal()
    {
        await SetupTwoStocks();

        var provider = Get<StockDataProvider>();

        // Dashboard: GetStockSummary should use Daily prices (same as StockDataProvider)
        var dashboardController = Get<SummaryPageController>();
        var summaryResult = await dashboardController.GetStockSummary();
        var summary = summaryResult.ShouldBeOkObjectResult<StockSummaryResponse>();

        // Both should use Daily close: Stock A: 10 * $110 = $1100, Stock B: 5 * $220 = $1100 => Total = $2200
        // (NOT FiveMinutes close which would give: 10 * $115 + 5 * $230 = $2300)
        summary.Total.ShouldBe(2200m);

        // StockDataProvider should match exactly
        var start = new DateOnly(2024, 1, 25);
        var end = new DateOnly(2024, 1, 26);
        var stockValues = await provider.GetDailyOwnedStockValue(start, end);
        var totalFromProvider = stockValues[start].EndOfDay.CurrentValue;

        totalFromProvider.ShouldBe(summary.Total);
    }
}
