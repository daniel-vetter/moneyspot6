using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;

namespace MoneySpot6.WebApp.Features.Ui.SimulationPage;

[ApiController]
[Route("api/[controller]")]
public class SimulationModelsController : Controller
{
    private readonly Db _db;
    private readonly SimulationRunner _simulationRunner;

    public SimulationModelsController(Db db, SimulationRunner simulationRunner)
    {
        _db = db;
        _simulationRunner = simulationRunner;
    }

    [HttpGet("GetAll")]
    [Produces<SimulationModelListItemResponse[]>]
    public async Task<ImmutableArray<SimulationModelListItemResponse>> GetAll()
    {
        return await _db.SimulationModels
            .Select(m => new SimulationModelListItemResponse
            {
                Id = m.Id,
                Name = m.Name
            })
            .ToImmutableArrayAsync();
    }

    [HttpGet("GetById")]
    [Produces<SimulationModelResponse>]
    public async Task<IActionResult> GetById(int id)
    {
        var model = await _db.SimulationModels
            .SingleOrDefaultAsync(x => x.Id == id);

        if (model == null)
            return NotFound();

        var latestRevision = await _db.SimulationModelRevisions
            .Where(r => r.SimulationModel.Id == id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        return Ok(new SimulationModelResponse
        {
            Id = model.Id,
            Name = model.Name,
            LatestRevisionId = latestRevision?.Id,
            OriginalCode = latestRevision?.OriginalCode ?? ""
        });
    }

    [HttpPut("Create")]
    [Produces<int>]
    [ProducesResponseType<SimulationModelValidationErrorResponse>(400)]
    public async Task<IActionResult> Create(NewSimulationModelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new SimulationModelValidationErrorResponse
            {
                MissingName = true
            });
        }

        var existingWithName = await _db.SimulationModels.AnyAsync(x => x.Name == request.Name);
        if (existingWithName)
        {
            return BadRequest(new SimulationModelValidationErrorResponse
            {
                NameAlreadyInUse = true
            });
        }

        var model = new DbSimulationModel
        {
            Name = request.Name
        };

        _db.SimulationModels.Add(model);
        await _db.SaveChangesAsync();

        var initialCode = request.IncludeSampleCode ? $$"""
        // Beispiel: Finanzsimulation mit monatlichem Gehalt, Ausgaben und ETF-Sparplan

        export function onInit(): InitialConfig {
            return {
                startDate: new DateOnly({{DateTime.Now.Year}}, 1, 1),
                endDate: new DateOnly({{DateTime.Now.Year + 50}}, 12, 31),
                startBalance: 10000,
                stocks: [
                    {
                        name: "MSCI World ETF",
                        startAmount: 0,
                        pricePredictor: new SPPLinearYearly(new DateOnly({{DateTime.Now.Year}}, 1, 1), 100, 7)
                    }
                ]
            };
        }

        export function onTick() {
            // Gehalt am 1. des Monats
            if (today.day === 1) {
                addTransaction("Gehalt", adjust(3500).from(start).to(today));
            }

            // Miete am 1. des Monats
            if (today.day === 1) {
                addTransaction("Miete", -adjust(1200).from(start).to(today));
            }

            // ETF-Sparplan am 15. des Monats
            if (today.day === 15 && balance > adjust(500).from(start).to(today)) {
                buyStocksFor("MSCI World ETF", adjust(500).from(start).to(today));
            }

            // Monatliche Ausgaben verteilt
            if (today.day === 10 || today.day === 20) {
                addTransaction("Lebensmittel", -adjust(200).from(start).to(today));
            }
        }
        """ : $$"""
        export function onInit(): InitialConfig {
            return {
                startDate: new DateOnly({{DateTime.Now.Year}}, 1, 1),
                endDate: new DateOnly({{DateTime.Now.Year + 50}}, 12, 31),
                startBalance: 10000
            };
        }

        export function onTick() {
            
        }
        """;

        var revision = new DbSimulationModelRevision
        {
            SimulationModel = model,
            CreatedAt = DateTimeOffset.UtcNow,
            OriginalCode = initialCode,
            CompiledCode = "",
            SourceMap = ""
        };

        _db.SimulationModelRevisions.Add(revision);
        await _db.SaveChangesAsync();

        return Ok(model.Id);
    }

    [HttpPost("Update")]
    [Produces<int>]
    public async Task<IActionResult> Update(UpdateSimulationModelRequest request)
    {
        var model = await _db.SimulationModels.FindAsync(request.Id);

        if (model == null)
            return NotFound();

        var newRevision = new DbSimulationModelRevision
        {
            SimulationModel = model,
            CreatedAt = DateTimeOffset.UtcNow,
            OriginalCode = request.OriginalCode,
            CompiledCode = request.CompiledCode,
            SourceMap = request.SourceMap
        };

        _db.SimulationModelRevisions.Add(newRevision);
        await _db.SaveChangesAsync();

        return Ok(newRevision.Id);
    }

    [HttpPost("Rename")]
    [ProducesResponseType<SimulationModelValidationErrorResponse>(400)]
    public async Task<IActionResult> Rename(RenameSimulationModelRequest request)
    {
        var model = await _db.SimulationModels.FindAsync(request.Id);

        if (model == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new SimulationModelValidationErrorResponse
            {
                MissingName = true
            });
        }

        var existingWithName = await _db.SimulationModels.AnyAsync(x => x.Name == request.Name && x.Id != request.Id);
        if (existingWithName)
        {
            return BadRequest(new SimulationModelValidationErrorResponse
            {
                NameAlreadyInUse = true
            });
        }

        model.Name = request.Name;
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("Run")]
    public async Task<IActionResult> Run(int revisionId)
    {
        await _simulationRunner.Run(revisionId);
        return Ok();
    }

    [HttpGet("GetRunResult")]
    [Produces<SimulationRunResultResponse>]
    public async Task<IActionResult> GetRunResult(int revisionId)
    {
        var revision = await _db.SimulationModelRevisions
            .SingleOrDefaultAsync(r => r.Id == revisionId);
        if (revision == null) return NotFound();

        var logs = await _db.SimulationLogs
            .Where(l => l.Revision.Id == revisionId)
            .Select(l => l.Message)
            .ToListAsync();

        var transactions = await _db.SimulationTransactions
            .Where(t => t.Revision.Id == revisionId)
            .OrderBy(t => t.Date)
            .Select(t => new SimulationTransactionResponse
            {
                Date = t.Date,
                Title = t.Title,
                Balance = t.Balance,
                Amount = t.Amount
            })
            .ToListAsync();

        var daySummaries = await _db.SimulationDaySummaries
            .Where(d => d.Revision.Id == revisionId)
            .OrderBy(d => d.Date)
            .Select(d => new SimulationDaySummaryResponse
            {
                Date = d.Date,
                Balance = d.Balance,
                Amount = d.Amount,
                TotalStockValue = d.TotalStockValue
            })
            .ToListAsync();

        return Ok(new SimulationRunResultResponse
        {
            Logs = logs,
            Transactions = transactions,
            DaySummaries = daySummaries
        });
    }

    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.SimulationModels.FindAsync(id);

        if (model == null)
            return NotFound();

        _db.SimulationModels.Remove(model);
        await _db.SaveChangesAsync();

        return Ok();
    }
}

[PublicAPI]
public record SimulationModelListItemResponse
{
    [Required] public int Id { get; set; }
    [Required] public required string Name { get; set; }
}

[PublicAPI]
public record SimulationModelResponse
{
    [Required] public int Id { get; set; }
    [Required] public required string Name { get; set; }
    public int? LatestRevisionId { get; set; }
    [Required] public required string OriginalCode { get; set; }
}

[PublicAPI]
public record NewSimulationModelRequest
{
    [Required] public required string Name { get; set; }
    [Required] public required bool IncludeSampleCode { get; set; }
}

[PublicAPI]
public record UpdateSimulationModelRequest
{
    [Required] public required int Id { get; set; }
    [Required] public required string OriginalCode { get; set; }
    [Required] public required string CompiledCode { get; set; }
    [Required] public required string SourceMap { get; set; }
}

[PublicAPI]
public record RenameSimulationModelRequest
{
    [Required] public required int Id { get; set; }
    [Required] public required string Name { get; set; }
}

[PublicAPI]
public record SimulationModelValidationErrorResponse
{
    [Required] public bool MissingName { get; set; }
    [Required] public bool NameAlreadyInUse { get; set; }
}

[PublicAPI]
public record SimulationRunResultResponse
{
    [Required] public required List<string> Logs { get; set; }
    [Required] public required List<SimulationTransactionResponse> Transactions { get; set; }
    [Required] public required List<SimulationDaySummaryResponse> DaySummaries { get; set; }
}

[PublicAPI]
public record SimulationTransactionResponse
{
    [Required] public required DateOnly Date { get; set; }
    [Required] public required string Title { get; set; }
    [Required] public required decimal Balance { get; set; }
    [Required] public required decimal Amount { get; set; }
}

[PublicAPI]
public record SimulationDaySummaryResponse
{
    [Required] public required DateOnly Date { get; set; }
    [Required] public required decimal Balance { get; set; }
    [Required] public required decimal Amount { get; set; }
    [Required] public required decimal TotalStockValue { get; set; }
}
