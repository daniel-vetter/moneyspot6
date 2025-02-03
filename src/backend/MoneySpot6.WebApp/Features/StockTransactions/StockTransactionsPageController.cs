using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;
using NJsonSchema;
using NJsonSchema.Annotations;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using MoneySpot6.WebApp.Features.Shared;

namespace MoneySpot6.WebApp.Features.StockTransactions;

[ApiController]
[Route("api/[controller]")]
public class StockTransactionsPageController : Controller
{
    private readonly Db _db;
    private readonly StockDataProvider _stockDataProvider;
    private readonly PortfolioProvider _portfolioProvider;

    public StockTransactionsPageController(Db db, StockDataProvider stockDataProvider, PortfolioProvider portfolioProvider)
    {
        _db = db;
        _stockDataProvider = stockDataProvider;
        _portfolioProvider = portfolioProvider;
    }

    [HttpGet("GetStockTransactions")]
    public async Task<ActionResult<ImmutableArray<StockTransactionResponse>>> GetStockTransactions() //TODO: StockId
    {
        var transactions = await _db.StockTransactions
            .AsNoTracking()
            .Select(x => new StockTransactionResponse
            {
                Id = x.Id,
                Amount = x.Amount,
                Date = x.Date,
                Price = x.Price,
                StockId = x.Stock.Id,
                StockName = x.Stock.Name
            })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Id)
            .ToImmutableArrayAsync();

        return Ok(transactions);
    }

    [HttpGet("GetPortfolio")]
    public async Task<ActionResult<ImmutableArray<PortfolioStockResponse>>> GetPortfolio()
    {
        return Ok((await _portfolioProvider.GetPortfolio()).Select(x => new PortfolioStockResponse
        {
            StockId = x.StockId,
            StockName = x.StockName,
            PurchaseAmount = x.PurchaseAmount,
            PurchasePrice = x.PurchasePrice,
            SoldAmount = x.SoldAmount,
            SoldPrice = x.SoldPrice,
            SoldTax = x.SoldTax,
            RemainingAmount = x.RemainingAmount,
            RemainingPrice = x.RemainingPrice,
            RemainingTax = x.RemainingTax,
            Purchases = x.Purchases.Select(y => new PortfolioStockPurchaseResponse
            {
                Date = y.Date,
                PurchaseAmount = y.PurchaseAmount,
                PurchasePrice = y.PurchasePrice,
                SoldAmount = y.SoldAmount,
                SoldPrice = y.SoldPrice,
                SoldTax = y.SoldTax,
                RemainingAmount = y.RemainingAmount,
                RemainingPrice = y.RemainingPrice,
                RemainingTax = y.RemainingTax
            }).ToImmutableArray()
        }).ToImmutableArray());
    }

    [HttpGet("GetStockTransaction")]
    public async Task<ActionResult<StockTransactionResponse>> GetStockTransaction(int id)
    {
        var transaction = await _db.StockTransactions
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new StockTransactionResponse
            {
                Id = x.Id,
                StockId = x.Stock.Id,
                StockName = x.Stock.Name,
                Amount = x.Amount,
                Price = x.Price,
                Date = x.Date,
            })
            .SingleOrDefaultAsync();

        return Ok(transaction);
    }

    [HttpPost("CreateNewTransaction")]
    public async Task<ActionResult> CreateNewTransaction(int stockId, decimal amount, decimal price, [BindRequired, JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly date)
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
            Date = date
        });
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("UpdateTransaction/{id}")]
    public async Task<ActionResult> UpdateTransaction(int id, int stockId, decimal amount, decimal price, [BindRequired, JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly date)
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
        transaction.Date = date;
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

public class PortfolioStockResponse
{
    [Required] public required int StockId { get; set; }
    [Required] public required string StockName { get; set; }
    [Required] public required decimal PurchaseAmount { get; set; }
    [Required] public required decimal PurchasePrice { get; set; }
    [Required] public required decimal SoldAmount { get; set; }
    [Required] public required decimal SoldPrice { get; set; }
    [Required] public required decimal SoldTax { get; set; }
    [Required] public required decimal RemainingAmount { get; set; }
    [Required] public required decimal RemainingPrice { get; set; }
    [Required] public required decimal RemainingTax { get; set; }
    [Required] public required ImmutableArray<PortfolioStockPurchaseResponse> Purchases { get; set; }
}

public class PortfolioStockPurchaseResponse
{
    [Required] public required DateOnly Date { get; set; }
    [Required] public required decimal PurchaseAmount { get; set; }
    [Required] public required decimal PurchasePrice { get; set; }
    [Required] public required decimal SoldAmount { get; set; }
    [Required] public required decimal SoldPrice { get; set; }
    [Required] public required decimal SoldTax { get; set; }
    [Required] public required decimal RemainingAmount { get; set; }
    [Required] public required decimal RemainingPrice { get; set; }
    [Required] public required decimal RemainingTax { get; set; }
}

public record StockTransactionResponse
{
    [Required] public int Id { get; init; }
    [Required] public required int StockId { get; init; }
    [Required] public required string StockName { get; init; }
    [Required] public required DateOnly Date { get; init; }
    [Required] public required decimal Amount { get; init; }
    [Required] public required decimal Price { get; init; }
}

public record StockListEntryResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required string Name { get; init; }
}