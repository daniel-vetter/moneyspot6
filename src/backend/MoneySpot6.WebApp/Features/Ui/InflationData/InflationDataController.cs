using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.Inflation;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.Ui.InflationData;

[ApiController]
[Route("api/[controller]")]
public class InflationDataController : Controller
{
    private readonly Db _db;
    private readonly ILogger<InflationDataController> _logger;
    private readonly InflationCalculator _inflationCalculator;

    public InflationDataController(Db db, ILogger<InflationDataController> logger, InflationCalculator inflationCalculator)
    {
        _db = db;
        _logger = logger;
        _inflationCalculator = inflationCalculator;
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

    [HttpGet("GetAll")]
    [ProducesResponseType<InflationDataResponse>(200)]
    public async Task<IActionResult> GetAll([FromQuery] int projectionYears)
    {
        await _inflationCalculator.EnsureConfigIsLoaded();

        var startDate = new DateOnly(2000, 1, 1);
        var endDate = new DateOnly(DateTime.Now.Year + projectionYears, 12, 1);

        var totalMonths = (endDate.Year - startDate.Year) * 12 + (endDate.Month - startDate.Month) + 1;
        var builder = ImmutableArray.CreateBuilder<InflationDataEntryWithProjectionResponse>(totalMonths);

        for (var currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddMonths(1))
        {
            var indexValue = _inflationCalculator.GetIndexForDate(currentDate, out var isProjected);

            builder.Add(new InflationDataEntryWithProjectionResponse
            {
                Year = currentDate.Year,
                Month = currentDate.Month,
                IndexValue = indexValue,
                IsProjected = isProjected
            });
        }

        var settings = await _db.InflationSettings.FirstOrDefaultAsync();

        return Ok(new InflationDataResponse
        {
            Entries = builder.ToImmutable(),
            DefaultRate = settings?.DefaultRate ?? 0m
        });
    }

    [HttpPost("CalculateAdjustedValue")]
    [ProducesResponseType<CalculateAdjustedValueResponse>(200)]
    public async Task<IActionResult> CalculateAdjustedValue(CalculateAdjustedValueRequest request)
    {
        await _inflationCalculator.EnsureConfigIsLoaded();

        var fromDate = new DateOnly(request.FromYear, request.FromMonth, 1);
        var toDate = new DateOnly(request.ToYear, request.ToMonth, 1);

        var adjustedValue = _inflationCalculator.CalculateInflationAdjustedValue(request.Value, fromDate, toDate);

        return Ok(new CalculateAdjustedValueResponse
        {
            AdjustedValue = adjustedValue
        });
    }
}

[PublicAPI]
public record UpdateDefaultRateRequest
{
    [Required] public required decimal DefaultRate { get; init; }
}

[PublicAPI]
public record InflationDataResponse
{
    [Required] public required ImmutableArray<InflationDataEntryWithProjectionResponse> Entries { get; init; }
    [Required] public required decimal DefaultRate { get; init; }
}

[PublicAPI]
public record InflationDataEntryWithProjectionResponse
{
    [Required] public required int Year { get; init; }
    [Required] public required int Month { get; init; }
    [Required] public required decimal IndexValue { get; init; }
    [Required] public required bool IsProjected { get; init; }
}

[PublicAPI]
public record CalculateAdjustedValueRequest
{
    [Required] public required decimal Value { get; init; }
    [Required] public required int FromYear { get; init; }
    [Required] public required int FromMonth { get; init; }
    [Required] public required int ToYear { get; init; }
    [Required] public required int ToMonth { get; init; }
}

[PublicAPI]
public record CalculateAdjustedValueResponse
{
    [Required] public required decimal AdjustedValue { get; init; }
}
