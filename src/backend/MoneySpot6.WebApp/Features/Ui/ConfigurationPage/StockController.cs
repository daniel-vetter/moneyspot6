using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.Ui.ConfigurationPage;

[ApiController]
[Route("api/[controller]")]
public class StockController : Controller
{
    private readonly Db _db;

    public StockController(Db db)
    {
        _db = db;
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

    [HttpGet("Get")]
    [ProducesResponseType<StockDetailsResponse>(200)]
    public async Task<IActionResult> Get(int id)
    {
        var stock = await _db.Stocks
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id);

        if (stock == null)
            return NotFound();

        return Ok(new StockDetailsResponse
        {
            Id = stock.Id,
            Name = stock.Name,
            Symbol = stock.Symbol
        });
    }

    [HttpPut("Create")]
    [ProducesResponseType<int>(200)]
    [ProducesResponseType<StockValidationErrorResponse>(400)]
    public async Task<IActionResult> Create(CreateStockRequest request)
    {
        var validationError = ValidateRequest(request.Name, request.Symbol);

        if (validationError != null)
            return BadRequest(validationError);

        // Check if name already exists
        var nameExists = await _db.Stocks
            .AnyAsync(x => x.Name == request.Name);

        if (nameExists)
        {
            return BadRequest(new StockValidationErrorResponse
            {
                NameAlreadyExists = true
            });
        }

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

    [HttpPost("Update")]
    [ProducesResponseType(200)]
    [ProducesResponseType<StockValidationErrorResponse>(400)]
    public async Task<IActionResult> Update(UpdateStockRequest request)
    {
        var stock = await _db.Stocks
            .SingleOrDefaultAsync(x => x.Id == request.Id);

        if (stock == null)
            return NotFound();

        var validationError = ValidateRequest(request.Name, request.Symbol);

        if (validationError != null)
            return BadRequest(validationError);

        // Check if name already exists (excluding current stock)
        var nameExists = await _db.Stocks
            .AnyAsync(x => x.Name == request.Name && x.Id != request.Id);

        if (nameExists)
        {
            return BadRequest(new StockValidationErrorResponse
            {
                NameAlreadyExists = true
            });
        }

        stock.Name = request.Name;
        stock.Symbol = request.Symbol;

        await _db.SaveChangesAsync();

        return Ok();
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

    private StockValidationErrorResponse? ValidateRequest(string name, string? symbol)
    {
        var error = new StockValidationErrorResponse();

        if (string.IsNullOrWhiteSpace(name))
            error.MissingName = true;

        return error.HasError() ? error : null;
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
public record StockDetailsResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required string Name { get; init; }
    public string? Symbol { get; init; }
}

[PublicAPI]
public record CreateStockRequest
{
    [Required] public required string Name { get; init; }
    public string? Symbol { get; init; }
}

[PublicAPI]
public record UpdateStockRequest
{
    [Required] public required int Id { get; init; }
    [Required] public required string Name { get; init; }
    public string? Symbol { get; init; }
}

[PublicAPI]
public record StockValidationErrorResponse
{
    public bool MissingName { get; set; }
    public bool NameAlreadyExists { get; set; }

    public bool HasError()
    {
        return MissingName || NameAlreadyExists;
    }
}
