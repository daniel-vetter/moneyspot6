using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace MoneySpot6.WebApp.Infrastructure;

public static class DbEx
{
    public static async Task<ImmutableArray<T>> ToImmutableArrayAsync<T>(this IQueryable<T> query)
    {
        return ImmutableCollectionsMarshal.AsImmutableArray(await query.ToArrayAsync());
    }

    public static async Task<ImmutableDictionary<TKey, T>> ToImmutableDictionaryAsync<TKey, T>(this IQueryable<T> query, Func<T, TKey> keySelector) where TKey : notnull
    {
        return (await query.ToDictionaryAsync(keySelector)).ToImmutableDictionary();
    }
}