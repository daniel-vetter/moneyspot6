using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.Ui.InflationData;

[ApiController]
[Route("api/[controller]")]
public class InflationDataController : Controller
{
    private readonly Db _db;
    private readonly ILogger<InflationDataController> _logger;

    public InflationDataController(Db db, ILogger<InflationDataController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("GetAll")]
    [ProducesResponseType<InflationDataListResponse>(200)]
    public async Task<IActionResult> GetAll()
    {
        var data = await _db.InflationData
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Select(x => new InflationDataEntryResponse
            {
                Id = x.Id,
                Year = x.Year,
                Month = x.Month,
                IndexValue = x.IndexValue
            })
            .ToArrayAsync();

        var settings = await _db.InflationSettings.FirstOrDefaultAsync();

        return Ok(new InflationDataListResponse
        {
            Entries = data.ToImmutableArray(),
            DefaultRate = settings?.DefaultRate ?? 0m
        });
    }

    [HttpPost("UpdateDefaultRate")]
    public async Task<IActionResult> UpdateDefaultRate(UpdateDefaultRateRequest request)
    {
        var settings = await _db.InflationSettings.FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new DbInflationSettings
            {
                DefaultRate = request.DefaultRate
            };
            _db.InflationSettings.Add(settings);
        }
        else
        {
            settings.DefaultRate = request.DefaultRate;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}

[PublicAPI]
public record InflationDataListResponse
{
    [Required] public required ImmutableArray<InflationDataEntryResponse> Entries { get; init; }
    [Required] public required decimal DefaultRate { get; init; }
}

[PublicAPI]
public record InflationDataEntryResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required int Year { get; init; }
    [Required] public required int Month { get; init; }
    [Required] public required decimal IndexValue { get; init; }
}

[PublicAPI]
public record UpdateDefaultRateRequest
{
    [Required] public required decimal DefaultRate { get; init; }
}
