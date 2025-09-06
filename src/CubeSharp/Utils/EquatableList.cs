using System.Collections;

namespace CubeSharp.Utils;

internal sealed class EquatableList<T>(IReadOnlyList<T> items)
    : IReadOnlyList<T>, IEquatable<EquatableList<T>>
{
    public int Count => Items.Count;

    private IReadOnlyList<T> Items { get; } = items;

    public T this[int index] => Items[index];

    public static bool operator ==(EquatableList<T> left, EquatableList<T> right) =>
        Equals(left, right);

    public static bool operator !=(EquatableList<T> left, EquatableList<T> right) =>
        !Equals(left, right);

    public IEnumerator<T> GetEnumerator() =>
        Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(EquatableList<T>? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return SequenceEqual(Items, other.Items);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((EquatableList<T>)obj);
    }

    public override int GetHashCode()
    {
        const int nullHashCode = 88_363_741;
        var result = nullHashCode;
        for (var index = 0; index < Items.Count; index++)
        {
            var item = Items[index];
            result = HashCode.Combine(result, item?.GetHashCode() ?? nullHashCode);
        }

        return result;
    }

    private static bool SequenceEqual(IReadOnlyList<T> first, IReadOnlyList<T> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            if (!System.Collections.Comparer.Equals(first[i], second[i]))
            {
                return false;
            }
        }

        return true;
    }
}
