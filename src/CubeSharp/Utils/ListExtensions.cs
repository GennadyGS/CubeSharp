namespace CubeSharp.Utils;

internal static class ListExtensions
{
    public static IReadOnlyList<T> ToEquatableList<T>(this IReadOnlyList<T> source) =>
        new EquatableList<T>(source);

    public static IReadOnlyList<T> SetValues<T>(
        this IReadOnlyList<T> source,
        IReadOnlyList<(int index, T value)> indexesAndValues)
    {
        var result = source.ToList();
        for (var i = 0; i < indexesAndValues.Count; i++)
        {
            result[indexesAndValues[i].index] = indexesAndValues[i].value;
        }

        return result;
    }

    public static IReadOnlyList<T> SetValues<T>(
        this IReadOnlyList<T> source,
        IReadOnlyList<int> indexes,
        IReadOnlyList<T> values)
    {
        if (indexes.Count < values.Count)
        {
            throw new ArgumentException($"No enough {nameof(indexes)} are specified.", nameof(indexes));
        }

        var result = source.ToList();
        for (var i = 0; i < values.Count; i++)
        {
            result[indexes[i]] = values[i];
        }

        return result;
    }

    public static IReadOnlyList<T> SetValue<T>(
        this IReadOnlyList<T> source,
        int index,
        T value)
    {
        var result = source.ToList();
        result[index] = value;
        return result;
    }

    public static IReadOnlyList<T> SelectAllAt<T>(
        this IReadOnlyList<T> source, IReadOnlyList<int> indexes) =>
        indexes
            .Select(index => source[index])
            .ToList();

    public static IReadOnlyList<T> RemoveAllAt<T>(
        this IReadOnlyList<T> source,
        IReadOnlyList<int> indexes)
    {
        var result = source.ToList();
        for (var i = indexes.Count - 1; i >= 0; i--)
        {
            result.RemoveAt(i);
        }

        return result;
    }

    /// <summary>
    /// Returns Cartesian product of given collections.
    /// </summary>
    /// <typeparam name="T">
    /// The data type of collection items.
    /// </typeparam>
    /// <param name="collections">
    /// Collection of collections where inner collections are considered
    /// as sets of data on which Cartesian product will be applied.
    /// </param>
    /// <returns>
    /// Returns a collection of collections where inner collections are elements of Cartesian product.
    /// </returns>
    public static IReadOnlyList<IReadOnlyList<T>> GetAllCombinations<T>(
        this IReadOnlyList<IReadOnlyList<T>> collections)
    {
        var numCols = collections.Count;
        var numRows = collections.Aggregate(1, (a, b) => a * b.Count);

        var results = CreateArrayOfArrays<T>(numRows, numCols);

        var repeatFactor = 1;
        for (var c = numCols - 1; c >= 0; c--)
        {
            for (var r = 0; r < numRows; r++)
            {
                results[r][c] = collections[c][(r / repeatFactor) % collections[c].Count];
            }

            repeatFactor *= collections[c].Count;
        }

        return results;
    }

    private static T[][] CreateArrayOfArrays<T>(int numRows, int numCols)
    {
        var results = new T[numRows][];
        for (var i = 0; i < numRows; i++)
        {
            results[i] = new T[numCols];
        }

        return results;
    }
}
