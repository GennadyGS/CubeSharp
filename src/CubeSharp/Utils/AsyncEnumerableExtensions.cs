namespace CubeSharp.Utils;

internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<TResult> SelectManyAsync<TSource, TCollection, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, IEnumerable<TCollection>> collectionSelector,
        Func<TSource, TCollection, TResult> resultSelector)
    {
        await foreach (var item in source)
        {
            foreach (var collectedItem in collectionSelector(item))
            {
                yield return resultSelector(item, collectedItem);
            }
        }
    }

    public static async Task<IReadOnlyDictionary<TKey, TValue>>
        ToDictionaryWithAggregationAsync<TSource, TKey, TValue>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector,
            Func<TValue, TValue, TValue> aggregator)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        await foreach (var item in source)
        {
            var key = keySelector(item);
            var newValue = valueSelector(item);
            result.AddOrUpdate(key, newValue, aggregator);
        }

        return result;
    }
}
