<Query Kind="Program">
  <Reference></Reference>
  <Reference Relative="..\..\..\..\..\WizNG.Analytics\src\Cube\bin\Debug\net8.0\WizNG.Analytics.Cube.dll">C:\Source\WK\WizNG.Analytics\src\Cube\bin\Debug\net8.0\WizNG.Analytics.Cube.dll</Reference>
  <Namespace>System.Dynamic</Namespace>
  <Namespace>WizNG.Analytics.Cube</Namespace>
</Query>

struct CountAndSum
{
    public CountAndSum(int count, decimal sum)
    {
        Count = count;
        Sum = sum;
    }

    public static CountAndSum Zero => new CountAndSum(0, 0);

    public int Count { get; }

    public decimal Sum { get; }

    public static CountAndSum Combine(CountAndSum left, CountAndSum right) =>
        new CountAndSum(left.Count + right.Count, left.Sum + right.Sum);
}

public static class Extensions
{
    public static IDictionary<string, object> ToDictionary(this object obj) =>
        obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                prop => prop.Name,
                prop => prop.GetValue(obj, null));

    public static IEnumerable<dynamic> ToTable<TIndex, T>(
        this CubeResult<TIndex, T> cubeResult,
        Index[] rowDimensionNumbers,
        Index[] columnDimensionNumbers)
    {
        if (rowDimensionNumbers
            .Concat(columnDimensionNumbers)
            .GroupBy(x => x)
            .Any(g => g.Count() > 1))
            {
                throw new InvalidOperationException("Duplicated indexes are not allowed");
            }
        return cubeResult
            .BreakdownByDimensions(rowDimensionNumbers)
            .Select(row => row
                .GetBoundDimensionsAndIndexes()
                .Select(item => KeyValuePair.Create(
                    item.dimension.Title ?? string.Empty,
                    (object)item.dimension[item.index].Title ?? item.index))
                .Concat(row
                    .BreakdownByDimensions(
                        ShiftIndexes(columnDimensionNumbers, rowDimensionNumbers, cubeResult.FreeDimensionCount))
                    .Select(column => KeyValuePair.Create(
                        column
                            .GetBoundDimensionsAndIndexes()[^columnDimensionNumbers.Length ..]
                            .Select(item => item.dimension[item.index].Title ?? item.index?.ToString())
                            .JoinStrings("."),
                        (object)column.GetValue())))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                .ToExpando());
    }

    private static Index[] ShiftIndexes(Index[] indexes, Index[] preceedingIndexes, int length) =>
        indexes
            .Select(index => index.GetOffset(length))
            .Select(index => 
                index - preceedingIndexes.Count(i => i.GetOffset(length) < index))
            .Select(dimensionNumber => (Index)dimensionNumber)
            .ToArray();

    public static string JoinStrings(
        this IEnumerable<string> strings,
        string separator) =>
        string.Join(separator, strings);

    public static ExpandoObject ToExpando<T>(this IEnumerable<KeyValuePair<string, T>> dictionary)
    {
        var expando = new ExpandoObject();
        var expandoDic = (IDictionary<string, object>)expando;
        foreach (var kvp in dictionary)
            expandoDic.Add(kvp.Key, kvp.Value);
        return expando;
    }
}