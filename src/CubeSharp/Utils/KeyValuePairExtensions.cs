namespace Cube.Utils;

internal static class KeyValuePairExtensions
{
    public static KeyValuePair<TKey, TValue> MapKey<TKey, TValue>(
        this KeyValuePair<TKey, TValue> source,
        Func<TKey, TKey> mapFunc) =>
        KeyValuePair.Create(mapFunc(source.Key), source.Value);
}
