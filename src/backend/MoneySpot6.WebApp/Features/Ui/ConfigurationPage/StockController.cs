using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.Stocks.PriceImport.YahooAdapter;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.Ui.ConfigurationPage;

[ApiController]
[Route("api/[controller]")]
public class StockController : Controller
{
    private readonly Db _db;
    private readonly YahooStockDataClient _yahooClient;

    public StockController(Db db, YahooStockDataClient yahooClient)
    {
        _db = db;
        _yahooClient = yahooClient;
    }

    [HttpGet("GetAll")]
    [ProducesResponseType<ImmutableArray<StockListResponse>>(200)]
    public async Task<IActionResult> GetAll()
    {
        var stocks = await _db.Stocks
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var response = stocks.Select(x => new StockListResponse
        {
            Id = x.Id,
            Name = x.Name,
            Symbol = x.Symbol,
            LastImportDaily = x.LastImportDaily,
            LastImportErrorDaily = x.LastImportErrorDaily
        }).ToImmutableArray();

        return Ok(response);
    }

    [HttpGet("Search")]
    [ProducesResponseType<ImmutableArray<StockSearchResponse>>(200)]
    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(ImmutableArray<StockSearchResponse>.Empty);

        var results = await _yahooClient.Search(query);

        var response = results
            .Select(x => new StockSearchResponse
            {
                Symbol = x.Symbol,
                Name = x.LongName ?? x.ShortName ?? x.Symbol,
                Exchange = x.Exchange,
                Type = x.Type
            })
            .ToImmutableArray();

        return Ok(response);
    }

    [HttpPut("Create")]
    [ProducesResponseType<int>(200)]
    public async Task<IActionResult> Create(CreateStockRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Symbol))
            return BadRequest();

        var newStock = new DbStock
        {
            Name = request.Name,
            Symbol = request.Symbol,
            LastImportDaily = null,
            LastImportErrorDaily = null,
            LastImport5Min = null,
            LastImportError5Min = null
        };

        _db.Stocks.Add(newStock);
        await _db.SaveChangesAsync();

        return Ok(newStock.Id);
    }

    [HttpDelete("Delete")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Delete(int id)
    {
        var stock = await _db.Stocks
            .SingleOrDefaultAsync(x => x.Id == id);

        if (stock == null)
            return NotFound();

        // Cascade delete: Remove all stock prices and transactions
        var stockPrices = await _db.StockPrices
            .Where(x => x.Stock.Id == id)
            .ToListAsync();

        var stockTransactions = await _db.StockTransactions
            .Where(x => x.Stock.Id == id)
            .ToListAsync();

        _db.StockPrices.RemoveRange(stockPrices);
        _db.StockTransactions.RemoveRange(stockTransactions);
        _db.Stocks.Remove(stock);

        await _db.SaveChangesAsync();

        return Ok();
    }
}

[PublicAPI]
public record StockListResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required string Name { get; init; }
    public string? Symbol { get; init; }
    public DateTimeOffset? LastImportDaily { get; init; }
    public string? LastImportErrorDaily { get; init; }
}

[PublicAPI]
public record CreateStockRequest
{
    [Required] public required string Name { get; init; }
    [Required] public required string Symbol { get; init; }
}

[PublicAPI]
public record StockSearchResponse
{
    [Required] public required string Symbol { get; init; }
    [Required] public required string Name { get; init; }
    public string? Exchange { get; init; }
    public string? Type { get; init; }
}
