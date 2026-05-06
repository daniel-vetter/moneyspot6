using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Core.Config;

[ScopedService]
public class KeyValueConfiguration
{
    private readonly Db _db;

    public KeyValueConfiguration(Db db)
    {
        _db = db;
    }

    public async Task<T> Get<T>(string key)
    {
        var entry = await ReadEntry<T>(key);
        if (entry == null)
            throw new InvalidOperationException($"Config entry '{key}' does not exist.");
        return Deserialize<T>(entry.Value);
    }

    public async Task<T> Get<T>(string key, T defaultValue)
    {
        var entry = await ReadEntry<T>(key);
        if (entry == null)
            return defaultValue;
        return Deserialize<T>(entry.Value);
    }

    private async Task<DbConfigEntry?> ReadEntry<T>(string key)
    {
        var entry = await _db.ConfigEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Key == key);
        if (entry == null)
            return null;

        var expectedType = TypeTagFor<T>();
        if (entry.Type != expectedType)
            throw new InvalidOperationException(
                $"Config entry '{key}' has type '{entry.Type}', requested as '{expectedType}'.");

        return entry;
    }

    public async Task Set<T>(string key, T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value), $"Config value for key '{key}' must not be null.");

        var typeTag = TypeTagFor<T>();
        var serialized = Serialize(value, typeTag);

        var entry = await _db.ConfigEntries.FirstOrDefaultAsync(e => e.Key == key);
        if (entry == null)
        {
            _db.ConfigEntries.Add(new DbConfigEntry { Key = key, Value = serialized, Type = typeTag });
        }
        else
        {
            if (entry.Type != typeTag)
                throw new InvalidOperationException(
                    $"Cannot change type of config entry '{key}' from '{entry.Type}' to '{typeTag}'.");
            entry.Value = serialized;
        }

        await _db.SaveChangesAsync();
    }

    private static string Serialize<T>(T value, string typeTag) => typeTag switch
    {
        "json" => JsonSerializer.Serialize(value),
        "datetime" => ((DateTime)(object)value!).ToString("o", CultureInfo.InvariantCulture),
        "datetimeoffset" => ((DateTimeOffset)(object)value!).ToString("o", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)!
    };

    private static T Deserialize<T>(string value)
    {
        var typeTag = TypeTagFor<T>();
        var underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        object parsed = typeTag switch
        {
            "json" => JsonSerializer.Deserialize<T>(value)!,
            "datetime" => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            "datetimeoffset" => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            _ => Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture)
        };

        return (T)parsed;
    }

    private static string TypeTagFor<T>()
    {
        var t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (t == typeof(bool)) return "bool";
        if (t == typeof(int)) return "int";
        if (t == typeof(long)) return "long";
        if (t == typeof(double)) return "double";
        if (t == typeof(decimal)) return "decimal";
        if (t == typeof(string)) return "string";
        if (t == typeof(DateTime)) return "datetime";
        if (t == typeof(DateTimeOffset)) return "datetimeoffset";
        return "json";
    }
}
