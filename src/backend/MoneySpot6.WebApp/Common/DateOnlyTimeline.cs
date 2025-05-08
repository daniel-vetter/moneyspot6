using System.Collections;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace MoneySpot6.WebApp.Common;

public static class ImmutableTimeline
{
    public static ImmutableTimeline<T> Create<T>(DateOnly start, DateOnly end, IEnumerable<T> entries)
    {
        return new ImmutableTimeline<T>(start, end, entries.ToImmutableArray());
    }

    public static ImmutableTimeline<T> CreateContinuous<T>(DateOnly start, DateOnly end, T startValue, IDictionary<DateOnly, T> entries)
    {
        var result = new T[end.DayNumber - start.DayNumber];
        var lastEntry = startValue;
        for (var cur = start; cur < end; cur = cur.AddDays(1))
        {
            if (entries.TryGetValue(cur, out var curEntry))
            {
                result[cur.DayNumber - start.DayNumber] = curEntry;
                lastEntry = curEntry;
            }
            else
                result[cur.DayNumber - start.DayNumber] = lastEntry;
        }
        return new ImmutableTimeline<T>(start, end, ImmutableCollectionsMarshal.AsImmutableArray(result));
    }

    public static ImmutableTimeline<T> Build<T>(DateOnly start, DateOnly end, Func<DateOnly, T> handler)
    {
        var r = new T[end.DayNumber - start.DayNumber];
        for (var cur = start; cur < end; cur = cur.AddDays(1)) 
            r[cur.DayNumber - start.DayNumber] = handler(cur);
        return Create(start, end, r);
    }

    public static ImmutableTimeline<T> Build<T>(DateOnly start, DateOnly end, T startValue, Func<DateOnly, T, T> handler)
    {
        var r = new T[end.DayNumber - start.DayNumber];
        for (var cur = start; cur < end; cur = cur.AddDays(1)) 
            r[cur.DayNumber - start.DayNumber] = handler(cur, cur == start ? startValue : r[cur.DayNumber - start.DayNumber - 1]);
        return Create(start, end, r);
    }
}

public readonly struct ImmutableTimeline<T> : IEnumerable<KeyValuePair<DateOnly, T>>
{
    public ImmutableTimeline(DateOnly start, DateOnly end, ImmutableArray<T> values)
    {
        Start = start;
        End = end;
        Values = values;

        if (values.Length != end.DayNumber - start.DayNumber)
            throw new ArgumentException("Invalid array length", nameof(values));
    }

    public DateOnly Start { get; }
    public DateOnly End { get; }
    public ImmutableArray<T> Values { get; }

    public T this[DateOnly date]
    {
        get
        {
            if (date < Start || date >= End)
                throw new ArgumentOutOfRangeException(nameof(date));
            return Values[date.DayNumber - Start.DayNumber];
        }
    }

    [MustDisposeResource]
    public IEnumerator<KeyValuePair<DateOnly, T>> GetEnumerator()
    {
        var @this = this;

        return Values
            .Select((x, i) => new KeyValuePair<DateOnly, T>(@this.Start.AddDays(i), x))
            .GetEnumerator();
    }

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}