using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace MoneySpot6.WebApp.Features.Stocks
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockChartPageController : Controller
    {
        private readonly Db _db;

        public StockChartPageController(Db db)
        {
            _db = db;
        }

        [HttpGet("GetStocks")]
        public async Task<ImmutableArray<StockResponse>> GetStocks()
        {
            return ImmutableCollectionsMarshal.AsImmutableArray(
                await _db.Stocks
                    .AsNoTracking()
                    .Select(x => new StockResponse
                    {
                        Id = x.Id,
                        Name = x.Name,
                    }).ToArrayAsync()
            );
        }

        [HttpGet("GetHistory")]
        public async Task<ImmutableArray<StockPriceResponse>> GetHistory(int stockId, DateTimeOffset? start, DateTimeOffset? end, StockPriceInterval interval)
        {
            IQueryable<DbStockPrice> query = _db.StockPrices
                .AsNoTracking()
                .Where(x => x.Stock.Id == stockId)
                .Where(x => x.Interval == interval);
            
            if (start.HasValue)
                query = query.Where(x => x.Timestamp >= start);

            if (end.HasValue)
                query = query.Where(x => x.Timestamp < end);

            var entries = await query
                .OrderBy(x => x.Timestamp)
                .ToArrayAsync();
            
            var r = ImmutableArray.CreateBuilder<StockPriceResponse>(entries.Length);
            foreach (var entry in entries)
            {
                r.Add(new StockPriceResponse
                {
                    Timestamp = entry.Timestamp,
                    Open = entry.Open,
                    Close = entry.Close,
                    High = entry.High,
                    Low = entry.Low,
                    Volume = entry.Volume
                });
            }

            return r.MoveToImmutable();
        }
    }

    public class StockResponse
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
    }

    public class StockPriceResponse
    {
        [Required] public required DateTimeOffset Timestamp { get; init; }
        [Required] public decimal Open { get; init; }
        [Required] public decimal Close { get; init; }
        [Required] public decimal High { get; init; }
        [Required] public decimal Low { get; init; }
        [Required] public decimal Volume { get; init; }
    }
}
