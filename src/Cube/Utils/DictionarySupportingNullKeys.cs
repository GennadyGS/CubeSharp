using System.Collections;

namespace Cube.Utils;

internal sealed class DictionarySupportingNullKeys<TKey, TValue>
    : IReadOnlyDictionary<TKey, TValue>
{
    private readonly IReadOnlyDictionary<TKey, TValue> _inner;
    private readonly bool _hasDefaultKey;
    private readonly TValue _valueForDefaultKey;
    private readonly IEqualityComparer<TKey> _equalityComparer = EqualityComparer<TKey>.Default;

    public DictionarySupportingNullKeys(IReadOnlyCollection<KeyValuePair<TKey, TValue>> keyValuePairs)
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
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _hasDefaultKey = defaultKeyValuePairs.Any();
        if (_hasDefaultKey)
        {
            _valueForDefaultKey = defaultKeyValuePairs.SingleOrDefault().Value;
        }
    }

    public int Count =>
        _hasDefaultKey
            ? _inner.Count + 1
            : _inner.Count;

    public IEnumerable<TKey> Keys =>
        _hasDefaultKey
            ? _inner.Keys.ConcatToItem(default)
            : _inner.Keys;

    public IEnumerable<TValue> Values =>
        _hasDefaultKey
            ? _inner.Values.ConcatToItem(_valueForDefaultKey)
            : _inner.Values;

    public TValue this[TKey key] =>
        TryGetValue(key, out var value)
            ? value
            : throw new KeyNotFoundException($"Key {key} is not found in dictionary");

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var keyValuePairs = _hasDefaultKey
            ? _inner.ConcatItem(KeyValuePair.Create((TKey)default, _valueForDefaultKey))
            : _inner;
        return keyValuePairs.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool ContainsKey(TKey key) =>
        _equalityComparer.Equals(key, default)
            ? _hasDefaultKey
            : _inner.ContainsKey(key);

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_equalityComparer.Equals(key, default))
        {
            value = _valueForDefaultKey;
            return _hasDefaultKey;
        }

        return _inner.TryGetValue(key, out value);
    }
}
