using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.StockTransactions;

[ApiController]
[Route("api/[controller]")]
public class StockTransactionsPageController : Controller
{
    private readonly Db _db;

    public StockTransactionsPageController(Db db)
    {
        _db = db;
    }
    
    [HttpGet("GetStockTransactions")]
    public async Task<ActionResult<ImmutableArray<StockTransactionResponse>>> GetStockTransactions()
    {
        var transactions = await _db.StockTransactions
            .AsNoTracking()
            .Select(x => new StockTransactionResponse
            {
                Id = x.Id,
                StockId = x.Stock.Id,
                StockName = x.Stock.Name,
                Amount = x.Amount,
                Price = x.Price,
                Timestamp = x.Timestamp,
            })
            .ToArrayAsync();

        return Ok(ImmutableCollectionsMarshal.AsImmutableArray(transactions));
    }

    [HttpPost("CreateNewTransaction")]
    public async Task<ActionResult> CreateNewTransaction(int stockId, decimal amount, decimal price, DateTimeOffset timestamp)
    {
        var stock = await _db.Stocks
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id == stockId);

        if (stock == null)
            return BadRequest("Invalid stock id");

        _db.StockTransactions.Add(new DbStockTransaction
        {
            Stock = stock,
            Amount = amount,
            Price = price,
            Timestamp = timestamp
        });
        await _db.SaveChangesAsync();

        return Ok();
    }
    
    [HttpPost("UpdateTransaction/{id}")]
    public async Task<ActionResult> UpdateNewTransaction(int id, int stockId, decimal amount, decimal price, DateTimeOffset timestamp)
    {
        var transaction = await _db.StockTransactions
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id == id);
        
        if (transaction == null)
            return BadRequest("Invalid stock id");
        
        var stock = await _db.Stocks
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id == stockId);

        if (stock == null)
            return BadRequest("Invalid stock id");

        transaction.Stock = stock;
        transaction.Amount = amount;
        transaction.Price = price;
        transaction.Timestamp = timestamp;
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("DeleteTransaction/{id}")]
    public async Task<ActionResult> DeleteStockTransactions(int id)
    {
        var transaction = await _db.StockTransactions
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id == id);
        
        if (transaction == null)
            return BadRequest("Invalid stock id");
        
        _db.StockTransactions.Remove(transaction);
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("GetStocks")]
    public async Task<ActionResult<ImmutableArray<StockListEntryResponse>>> GetStocks()
    {
        var stocks = await _db.Stocks
            .AsNoTracking()
            .Select(x => new StockListEntryResponse
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToArrayAsync();
        
        return ImmutableCollectionsMarshal.AsImmutableArray(stocks);
    }
}

public record StockTransactionResponse
{
    [Required] public int Id { get; init; }
    [Required] public required int StockId { get; init; }
    [Required] public required string StockName { get; init; }
    [Required] public required DateTimeOffset Timestamp { get; init; }
    [Required] public required decimal Amount { get; init; }
    [Required] public required decimal Price { get; init; }
}

public record StockListEntryResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required string Name { get; init; }
}