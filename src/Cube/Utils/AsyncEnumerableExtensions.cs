namespace Cube.Utils;

internal static class AsyncEnumerableExtensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Minor Code Smell",
        "S4261:Methods should be named according to their synchronicities",
        Justification = "Method name should match LINQ conventions")]
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
