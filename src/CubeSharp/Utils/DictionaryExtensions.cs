namespace Cube.Utils;

internal static class DictionaryExtensions
{
    public static void AddOrUpdate<TKey, TValue>(
        this IDictionary<TKey, TValue> result,
        TKey key,
        TValue value,
        Func<TValue, TValue, TValue> updateFunc)
    {
        if (result.TryGetValue(key, out var oldValue))
        {
            result[key] = updateFunc(oldValue, value);
        }
        else
        {
            result.Add(key, value);
        }
    }
}
