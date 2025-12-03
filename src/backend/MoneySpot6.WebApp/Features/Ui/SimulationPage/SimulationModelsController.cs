using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

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
    [Produces<SimulationModelResponse[]>]
    public async Task<ImmutableArray<SimulationModelResponse>> GetAll()
    {
        return (await _db.SimulationModels.ToListAsync()).Select(x => new SimulationModelResponse
        {
            Id = x.Id,
            Name = x.Name,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            OriginalCode = x.OriginalCode,
            HasSyntaxErrors = x.HasSyntaxIssues
        }).ToImmutableArray();
    }

    [HttpGet("GetById")]
    [Produces<SimulationModelResponse>]
    public async Task<IActionResult> GetById(int id)
    {
        var model = await _db.SimulationModels.FindAsync(id);

        if (model == null)
            return NotFound();

        return Ok(new SimulationModelResponse
        {
            Id = model.Id,
            Name = model.Name,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            OriginalCode = model.OriginalCode,
            HasSyntaxErrors = model.HasSyntaxIssues
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
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            OriginalCode = request.OriginalCode,
            CompiledCode = request.CompiledCode,
            SourceMap = request.SourceMap,
            HasSyntaxIssues = false
        };

        _db.SimulationModels.Add(model);
        await _db.SaveChangesAsync();

        return Ok(model.Id);
    }

    [HttpPost("Update")]
    [ProducesResponseType<SimulationModelValidationErrorResponse>(400)]
    public async Task<IActionResult> Update(UpdateSimulationModelRequest request)
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
        model.StartDate = request.StartDate;
        model.EndDate = request.EndDate;
        model.OriginalCode = request.OriginalCode;
        model.CompiledCode = request.CompiledCode;
        model.SourceMap = request.SourceMap;

        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("Run")]
    [Produces<int>]
    public async Task<IActionResult> Run(int id)
    {
        var runId = await _simulationRunner.Run(id);
        return Ok(runId);
    }

    [HttpGet("GetRunResult")]
    [Produces<SimulationRunResultResponse>]
    public async Task<IActionResult> GetRunResult(int runId)
    {
        var run = await _db.SimulationRuns
            .Include(r => r.Logs)
            .Include(r => r.Transactions)
            .Include(r => r.DaySummaries)
            .SingleOrDefaultAsync(r => r.Id == runId);
        if (run == null) return NotFound();

        return Ok(new SimulationRunResultResponse
        {
            Logs = run.Logs.Select(l => l.Message).ToList(),
            Transactions = run.Transactions.Select(t => new SimulationTransactionResponse
            {
                Date = t.Date,
                Title = t.Title,
                Balance = t.Balance,
                Amount = t.Amount
            }).ToList(),
            DaySummaries = run.DaySummaries.Select(d => new SimulationDaySummaryResponse
            {
                Date = d.Date,
                Balance = d.Balance,
                Amount = d.Amount
            }).ToList()
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
public record SimulationModelResponse
{
    [Required] public int Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required DateOnly StartDate { get; set; }
    [Required] public required DateOnly EndDate { get; set; }
    [Required] public required string OriginalCode { get; set; }
    [Required] public required bool HasSyntaxErrors { get; set; }
}

[PublicAPI]
public record NewSimulationModelRequest
{
    [Required] public required string Name { get; set; }
    [Required] public required DateOnly StartDate { get; set; }
    [Required] public required DateOnly EndDate { get; set; }
    [Required] public required string OriginalCode { get; set; }
    [Required] public required string CompiledCode { get; set; }
    [Required] public required string SourceMap { get; set; }
}

[PublicAPI]
public record UpdateSimulationModelRequest
{
    [Required] public required int Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required DateOnly StartDate { get; set; }
    [Required] public required DateOnly EndDate { get; set; }
    [Required] public required string OriginalCode { get; set; }
    [Required] public required string CompiledCode { get; set; }
    [Required] public required string SourceMap { get; set; }
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
}
