using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Cube.Utils;

internal sealed class DictionarySupportingNullKeys<TKey, TValue>
    : IReadOnlyDictionary<TKey?, TValue>
    where TKey : notnull
{
    private readonly IReadOnlyDictionary<TKey, TValue> _inner;
    private readonly bool _hasDefaultKey;
    private readonly TValue _valueForDefaultKey;
    private readonly IEqualityComparer<TKey> _equalityComparer = EqualityComparer<TKey>.Default;

    public DictionarySupportingNullKeys(IReadOnlyCollection<KeyValuePair<TKey?, TValue>> keyValuePairs)
    {
        var defaultKeyValuePairs = keyValuePairs
            .Where(kvp => _equalityComparer.Equals(kvp.Key, default))
            .ToList();
        if (defaultKeyValuePairs.Count > 1)
        {
            throw new ArgumentException("Default key is duplicated.", nameof(keyValuePairs));
        }

        _inner = keyValuePairs
            .Where(kvp => !_equalityComparer.Equals(kvp.Key, default))
            .ToDictionary(kvp => kvp.Key!, kvp => kvp.Value);
        _hasDefaultKey = defaultKeyValuePairs.Any();
        _valueForDefaultKey = _hasDefaultKey
            ? defaultKeyValuePairs.SingleOrDefault().Value
            : default!;
    }

    public int Count =>
        _hasDefaultKey
            ? _inner.Count + 1
            : _inner.Count;

    public IEnumerable<TKey?> Keys =>
        _hasDefaultKey
            ? _inner.Keys.ConcatToItem(default)
            : _inner.Keys;

    public IEnumerable<TValue> Values =>
        _hasDefaultKey
            ? _inner.Values.ConcatToItem(_valueForDefaultKey)
            : _inner.Values;

    public TValue this[TKey? key] =>
        TryGetValue(key, out var value)
            ? value
            : throw new KeyNotFoundException($"Key {key} is not found in dictionary");

    public IEnumerator<KeyValuePair<TKey?, TValue>> GetEnumerator() =>
        _inner
            .Select(kvp => KeyValuePair.Create((TKey?)kvp.Key, kvp.Value))
            .AppendIf(KeyValuePair.Create((TKey?)default, _valueForDefaultKey), _hasDefaultKey)
            .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool ContainsKey(TKey? key) =>
        _equalityComparer.Equals(key, default)
            ? _hasDefaultKey
            : _inner.ContainsKey(key!);

    public bool TryGetValue(TKey? key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_equalityComparer.Equals(key, default))
        {
            value = _valueForDefaultKey;
            return _hasDefaultKey;
        }

        return _inner.TryGetValue(key!, out value);
    }
}
