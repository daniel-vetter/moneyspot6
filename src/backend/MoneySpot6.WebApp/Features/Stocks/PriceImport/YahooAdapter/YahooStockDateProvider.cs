using System.Collections.Immutable;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Stocks.PriceImport.YahooAdapter;

[SingletonService]
public class YahooStockDateProvider
{
    public async Task<ImmutableArray<StockPrice>> Get(DateTimeOffset start, DateTimeOffset end, string symbol, StockPriceInterval interval)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Host = "query1.finance.yahoo.com";
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
        var response = await client.GetAsync($"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?period1={start.ToUnixTimeSeconds()}&period2={end.ToUnixTimeSeconds()}&interval={(interval == StockPriceInterval.FiveMinutes ? "5m" : "1d")}&includePrePost=true&events=div%7Csplit%7Cearn&&lang=de-DE&region=DE");
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadFromJsonAsync<Response>();
        if (responseJson == null)
            throw new Exception("Response was null");

        if (responseJson.Chart.Error != null)
            throw new Exception("JSON contained error: " + responseJson.Chart.Error);

        if (responseJson.Chart.Result == null)
            throw new Exception("No result found");

        if (responseJson.Chart.Result.Length != 1)
            throw new Exception("Json contained 0 or more than 1 result.");

        var result = responseJson.Chart.Result.Single();

        var timestamps = result.Timestamp;
        if (timestamps == null)
            return ImmutableArray<StockPrice>.Empty;

        var quote = result.Indicators.Quote.Single();

        if (quote.Close == null || quote.Open == null || quote.High == null || quote.Low == null || quote.Volume == null)
            throw new Exception("One dataset is missing");

        if (timestamps.Length != quote.Open.Length ||
            timestamps.Length != quote.Close.Length ||
            timestamps.Length != quote.High.Length ||
            timestamps.Length != quote.Low.Length ||
            timestamps.Length != quote.Volume.Length)
            throw new Exception("Dataset length does not match");

        var r = ImmutableArray.CreateBuilder<StockPrice>();
        for (var i = 0; i < timestamps.Length; i++)
        {
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]);
            var open = quote.Open[i];
            var close = quote.Close[i];
            var high = quote.High[i];
            var low = quote.Low[i];
            var volume = quote.Volume[i];

            if (!open.HasValue || !close.HasValue || !high.HasValue || !low.HasValue || !volume.HasValue)
                continue;

            r.Add(new StockPrice(timestamp, open.Value, close.Value, low.Value, high.Value, volume.Value));
        }

        return r.ToImmutable();
    }


    class Response
    {
        public required ChartResponse Chart { get; init; }
    }

    class ChartResponse
    {
        public ChartResultResponse[]? Result { get; init; }
        public required string? Error { get; init; }
    }

    class ChartResultResponse
    {
        public required ChartResultMetadataResponse Meta { get; init; }
        public long[]? Timestamp { get; init; }
        public required ChartResultIndicatorResponse Indicators { get; init; }
    }

    internal class ChartResultMetadataResponse
    {
        public required string Currency { get; init; }
        public required string ExchangeName { get; init; }
        public required string ExchangeTimezoneName { get; init; }
    }

    class ChartResultIndicatorResponse
    {
        public required ChartResultIndicatorQuoteResponse[] Quote { get; init; }
    }
    
    class ChartResultIndicatorQuoteResponse
    {
        public decimal?[]? Open { get; init; }
        public decimal?[]? Close { get; init; }
        public decimal?[]? High { get; init; }
        public decimal?[]? Low { get; init; }
        public int?[]? Volume { get; init; }
    }
}

public record StockPrice(DateTimeOffset Timestamp, decimal Open, decimal Close, decimal Low, decimal High, int Volume);