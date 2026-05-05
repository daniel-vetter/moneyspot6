using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Features.Core.Config;
using MoneySpot6.WebApp.Features.Core.Inflation;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.Ui.InflationData;

[ApiController]
[Route("api/[controller]")]
public class InflationDataController : Controller
{
    private readonly InflationCalculator _inflationCalculator;
    private readonly IConfigService _config;

    public InflationDataController(InflationCalculator inflationCalculator, IConfigService config)
    {
        _inflationCalculator = inflationCalculator;
        _config = config;
    }

    [HttpPost("UpdateDefaultRate")]
    public async Task<IActionResult> UpdateDefaultRate(UpdateDefaultRateRequest request)
    {
        await _config.Set(InflationCalculator.DefaultRateConfigKey, request.DefaultRate);
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

        var defaultRate = await _config.Get(InflationCalculator.DefaultRateConfigKey, InflationCalculator.DefaultRateFallback);

        return Ok(new InflationDataResponse
        {
            Entries = builder.ToImmutable(),
            DefaultRate = defaultRate
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
