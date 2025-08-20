namespace CubeSharp.Utils;

internal static class EnumerableExtensions
{
    public static IEnumerable<T> AppendIf<T>(this IEnumerable<T> source, T item, bool condition) =>
        condition
            ? source.Append(item)
            : source;

    public static IEnumerable<T> ConcatToItem<T>(this IEnumerable<T> source, T item) =>
        new[] { item }.Concat(source);

    public static bool HasDuplicatesBy<TSource, T>(
        this IEnumerable<TSource> source,
        Func<TSource, T> selector) =>
        source
            .GroupBy(selector)
            .Any(g => g.Count() > 1);

    public static IReadOnlyDictionary<TKey?, TSource> ToDictionarySupportingNullKeys<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey?> keySelector)
        where TKey : notnull =>
        source.ToDictionarySupportingNullKeys(keySelector, item => item);

    public static IReadOnlyDictionary<TKey?, TValue>
        ToDictionarySupportingNullKeys<TSource, TKey, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey?> keySelector,
            Func<TSource, TValue> valueSelector)
        where TKey : notnull
    {
        var keyValuePairs = source
            .Select(item => KeyValuePair.Create(keySelector(item), valueSelector(item)))
            .ToList();
        return new DictionarySupportingNullKeys<TKey, TValue>(keyValuePairs);
    }

    public static IReadOnlyDictionary<TKey, TValue>
        ToDictionaryWithAggregation<TSource, TKey, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector,
            Func<TValue, TValue, TValue> aggregator)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            var newValue = valueSelector(item);
            result.AddOrUpdate(key, newValue, aggregator);
        }

        return result;
    }
}
