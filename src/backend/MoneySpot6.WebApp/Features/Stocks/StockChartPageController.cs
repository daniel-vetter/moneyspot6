using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

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
        public async Task<ImmutableArray<StockPriceResponse>> GetHistory(int stockId, DateOnly? start, DateOnly? end)
        {
            IQueryable<DbStockPrice> query = _db.StockPrices
                .AsNoTracking()
                .Where(x => x.Stock.Id == stockId);
            
            if (start.HasValue)
                query = query.Where(x => x.Date >= start);

            if (end.HasValue)
                query = query.Where(x => x.Date < end);

            var entries = await query
                .OrderBy(x => x.Date)
                .ToArrayAsync();
            
            var timestamps = ImmutableArray.CreateBuilder<long>(entries.Length);
            var prices = ImmutableArray.CreateBuilder<decimal>(entries.Length);

            var r = ImmutableArray.CreateBuilder<StockPriceResponse>(entries.Length);
            foreach (var entry in entries)
            {
                r.Add(new StockPriceResponse
                {
                    Date = entry.Date,
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
        public required DateOnly Date { get; init; }
        public decimal Open { get; init; }
        public decimal Close { get; init; }
        public decimal High { get; init; }
        public decimal Low { get; init; }
        public decimal Volume { get; init; }
    }
}
