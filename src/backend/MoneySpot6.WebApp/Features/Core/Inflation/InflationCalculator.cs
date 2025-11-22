using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Core.Inflation;

[ScopedService]
public class InflationCalculator
{
    private readonly Db _db;
    private double _monthlyRate;
    private Dictionary<YearMonth, decimal>? _indexValues;

    public InflationCalculator(Db db)
    {
        _db = db;
    }

    public async Task EnsureConfigIsLoaded()
    {
        if (_indexValues != null)
            return;

        _indexValues = await _db.InflationData
            .AsNoTracking()
            .ToDictionaryAsync(x => new YearMonth(x.Year, x.Month), x => x.IndexValue);

        var settings = await _db.InflationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (settings == null)
            throw new InvalidOperationException("No default inflation rate configured. Database may not be initialized.");

        // Convert annual inflation rate to monthly rate: monthly_rate = (1 + annual_rate)^(1/12) - 1
        _monthlyRate = Math.Pow(1 + (double)((double)settings.DefaultRate / 100.0), 1.0 / 12.0) - 1;
    }

    /// <summary>
    /// Calculates the inflation-adjusted value between two dates.
    /// Works in both directions (forward and backward in time).
    /// </summary>
    /// <param name="value">The original value</param>
    /// <param name="fromDate">The start date</param>
    /// <param name="toDate">The target date</param>
    /// <returns>The inflation-adjusted value</returns>
    public decimal CalculateInflationAdjustedValue(decimal value, DateOnly fromDate, DateOnly toDate)
    {
        if (_indexValues == null)
            throw new InvalidOperationException("Config must be set before calculating inflation");

        if (fromDate == toDate)
            return value;

        var startIndex = GetIndexForDate(fromDate);
        var endIndex = GetIndexForDate(toDate);

        return value * (endIndex / startIndex);
    }

    public decimal GetIndexForDate(DateOnly date)
    {
        return GetIndexForDate(date, out _);
    }

    public decimal GetIndexForDate(DateOnly date, out bool isPredicted)
    {
        if (_indexValues == null)
            throw new InvalidOperationException("Config must be set before calculating inflation");

        // Try to find exact match for year and month
        if (_indexValues.TryGetValue(new (date.Year, date.Month), out var indexValue))
        {
            isPredicted = false;
            return indexValue;
        }

        // Find the nearest known VPI value (regardless of direction)
        var targetMonthIndex = date.Year * 12 + date.Month;
        var nearestEntry = _indexValues
            .OrderBy(x => Math.Abs((x.Key.Year * 12 + x.Key.Month) - targetMonthIndex))
            .FirstOrDefault();

        // Calculate the time difference in months (positive = future, negative = past)
        var nearestDate = new YearMonth(nearestEntry.Key.Year, nearestEntry.Key.Month);
        var targetDate = new YearMonth(date.Year, date.Month);
        var monthsDiff = targetDate.Index - nearestDate.Index;

        // Estimate the VPI using monthly compound interest
        // Works for both forward (positive months) and backward (negative months) extrapolation
        var estimatedIndex = nearestEntry.Value * (decimal)Math.Pow(1 + _monthlyRate, monthsDiff);

        isPredicted = true;
        return estimatedIndex;
    }


    private record struct YearMonth(int Year, int Month)
    {
        public int Index => Year * 12 + Month;
    }
}
