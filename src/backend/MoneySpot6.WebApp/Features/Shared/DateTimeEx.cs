namespace MoneySpot6.WebApp.Features.Shared;

public static class DateTimeEx
{
    static DateTimeOffset ToDateTimeOffsetUtc(this DateOnly dateOnly, TimeZoneInfo timeZone)
    {
        return new DateTimeOffset(dateOnly, TimeOnly.MinValue, timeZone.GetUtcOffset(dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))).ToUniversalTime();
    }

    static DateOnly ToDateOnly(this DateTimeOffset dateTimeOffset, TimeZoneInfo timeZone)
    {
        var r = TimeZoneInfo.ConvertTime(dateTimeOffset, timeZone);
        return new DateOnly(r.Year, r.Month, r.Day);
    }
}