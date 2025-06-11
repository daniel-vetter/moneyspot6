namespace MoneySpot6.WebApp.Common;

public static class StringEx
{
    public static string? TrimToNull(this string? value)
    {
        if (value == null) 
            return null;

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    public static string TrimToEmptyString(this string? value)
    {
        if (value == null)
            return "";

        return value.Trim();
    }
}