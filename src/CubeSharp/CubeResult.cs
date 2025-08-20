using Cube.Utils;

namespace Cube;

/// <summary>
/// Represents result of the data cube generation.
/// </summary>
/// <typeparam name="TIndex">The type of the dimension index.</typeparam>
/// <typeparam name="T">The type of the aggregation result.</typeparam>
public sealed class CubeResult<TIndex, T>
    where TIndex : notnull
{
    internal CubeResult(
        IReadOnlyDictionary<IReadOnlyList<TIndex?>, T> resultMap,
        T defaultValue,
        IReadOnlyList<Dimension<TIndex>> allDimensions)
        : this(resultMap, defaultValue, allDimensions, Array.Empty<(int, TIndex?)>())
    {
    }

    private CubeResult(
        IReadOnlyDictionary<IReadOnlyList<TIndex?>, T> resultMap,
        T defaultValue,
        IReadOnlyList<Dimension<TIndex>> allDimensions,
        IReadOnlyList<(int number, TIndex? index)> boundDimensionNumbersAndIndexes)
    {
        IReadOnlyList<int> GetFreeDimensionNumbers()
        {
            var boundDimensionNumbers = boundDimensionNumbersAndIndexes
                .Select(item => item.number)
                .ToHashSet();
            return Enumerable.Range(0, allDimensions.Count)
                .Where(number => !boundDimensionNumbers.Contains(number))
                .ToList();
        }

        ResultMap = resultMap;
        DefaultValue = defaultValue;
        AllDimensions = allDimensions;
        BoundDimensionNumbersAndIndexes = boundDimensionNumbersAndIndexes;
        FreeDimensionNumbers = GetFreeDimensionNumbers();
        Key = new TIndex?[allDimensions.Count].SetValues(BoundDimensionNumbersAndIndexes);
    }

    /// <summary>Gets the number of the free dimensions.</summary>
    /// <value>The count of the free dimensions.</value>
    public int FreeDimensionCount => AllDimensions.Count - BoundDimensionNumbersAndIndexes.Count;

    /// <summary> Gets the number of the bound dimensions.</summary>
    /// <value>The number of the bound dimensions.</value>
    public int BoundDimensionCount => BoundDimensionNumbersAndIndexes.Count;

    private IReadOnlyDictionary<IReadOnlyList<TIndex?>, T> ResultMap { get; }

    private T DefaultValue { get; }

    private IReadOnlyList<Dimension<TIndex>> AllDimensions { get; }

    private IReadOnlyList<int> FreeDimensionNumbers { get; }

    private IReadOnlyList<(int number, TIndex? index)> BoundDimensionNumbersAndIndexes { get; }

    private IReadOnlyList<TIndex?> Key { get; }

    /// <summary>
    /// Slices the current instance of <see cref="CubeResult{TIndex, T}"/>
    /// by the <paramref name="index"/> in the first free dimension.
    /// </summary>
    /// <value>
    /// The new instance of the <see cref="CubeResult{TIndex, T}"/> sliced by
    /// the index <paramref name="index"/> in the first free dimension.
    /// </value>
    /// <param name="index">
    /// The index in the first free dimension
    /// by which to slice the <see cref="CubeResult{TIndex, T}"/>.
    /// </param>
    public CubeResult<TIndex, T> this[TIndex? index] => Slice(0, index);

    /// <summary>
    /// Slices the current instance of <see cref="CubeResult{TIndex, T}"/>
    /// by the <paramref name="index"/> in the free dimension
    /// with the number <paramref name="dimensionNumber"/>.
    /// </summary>
    /// <param name="dimensionNumber">
    /// The free dimension number by which to slice the <see cref="CubeResult{TIndex, T}"/>.</param>
    /// <param name="index">
    /// The index in the free dimension number <paramref name="dimensionNumber"/>
    /// by which to slice the <see cref="CubeResult{TIndex, T}"/>.
    /// </param>
    /// <returns>
    /// The new instance of the <see cref="CubeResult{TIndex, T}"/> sliced by
    /// the <paramref name="index"/> in the specified free dimension.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Dimension number is out of range.
    /// </exception>
    public CubeResult<TIndex, T> Slice(Index dimensionNumber, TIndex? index)
    {
        var freeDimensionNumber = GetFreeDimensionNumber(dimensionNumber);
        var updatedBoundDimensionNumbersAndIndexes =
            BoundDimensionNumbersAndIndexes.Append((freeDimensionNumber, index)).ToList();
        return new CubeResult<TIndex, T>(
            ResultMap, DefaultValue, AllDimensions, updatedBoundDimensionNumbersAndIndexes);
    }

    /// <summary>
    /// Slices the current instance of <see cref="CubeResult{TIndex, T}"/>
    /// by the indexes and the free dimension numbers specified
    /// by <paramref name="dimensionsAndIndexes"/>.
    /// </summary>
    /// <param name="dimensionsAndIndexes">
    /// The indexes and the corresponding free dimension numbers
    /// by which to slice the <see cref="CubeResult{TIndex, T}"/>.
    /// </param>
    /// <returns>
    /// The new instance of the <see cref="CubeResult{TIndex, T}"/> sliced by
    /// the indexes and the corresponding free dimension
    /// numbers <paramref name="dimensionsAndIndexes"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Dimension numbers should not contain duplicates.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Dimension number is out of range.
    /// </exception>
    public CubeResult<TIndex, T> Slice(
        params (Index dimensionNumber, TIndex? index)[] dimensionsAndIndexes)
    {
        if (dimensionsAndIndexes.HasDuplicatesBy(item => item.dimensionNumber))
        {
            throw new ArgumentException("Dimension numbers should not contain duplicates.");
        }

        var boundDimensionsAndIndexesDelta = dimensionsAndIndexes
            .Select(item => (GetFreeDimensionNumber(item.dimensionNumber), item.index))
            .ToList();
        var updatedBoundDimensionNumbersAndIndexes =
            BoundDimensionNumbersAndIndexes.Concat(boundDimensionsAndIndexesDelta).ToList();
        return new CubeResult<TIndex, T>(
            ResultMap, DefaultValue, AllDimensions, updatedBoundDimensionNumbersAndIndexes);
    }

    /// <summary>
    /// Gets the aggregated value of the whole cube result.
    /// </summary>
    /// <returns>
    /// The aggregated value of the whole cube result <see cref="CubeResult{TIndex, T}"/>.
    /// </returns>
    public T GetValue() => GetValueFromResultMap(Key);

    /// <summary>
    /// Gets the aggregated value by index in first free dimension.
    /// </summary>
    /// <param name="index">
    /// The index in the first free dimension.
    /// </param>
    /// <returns>
    /// The aggregated value for cell of <see cref="CubeResult{TIndex, T}"/>
    /// corresponding to specified <paramref name="index"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Cube result does not have any free dimensions.
    /// </exception>
    public T GetValue(TIndex? index)
    {
        if (FreeDimensionCount <= 0)
        {
            throw new InvalidOperationException("Cube result does not have any free dimensions.");
        }

        var updatedKey = Key.SetValue(FreeDimensionNumbers[0], index);
        return GetValueFromResultMap(updatedKey);
    }

    /// <summary>
    /// Gets the aggregated value by collection of indexes in free dimensions.
    /// </summary>
    /// <param name="indexes">
    /// The indexes in free dimensions.
    /// </param>
    /// <remarks>
    /// The number of <paramref name="indexes"/> should be less than or equal
    /// to the number of the free dimensions.
    /// </remarks>
    /// <returns>
    /// The aggregated value for cell of <see cref="CubeResult{TIndex, T}"/>
    /// corresponding to specified <paramref name="indexes"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Number of specified indexes is greater than number of dimensions.
    /// </exception>
    public T GetValue(params TIndex?[] indexes)
    {
        if (indexes.Length > FreeDimensionCount)
        {
            throw new ArgumentException(
                $"Number of specified {nameof(indexes)} is greater than number of dimensions.",
                nameof(indexes));
        }

        var updatedKey = Key.SetValues(FreeDimensionNumbers, indexes);
        return GetValueFromResultMap(updatedKey);
    }

    /// <summary>Gets the free dimension by its number.</summary>
    /// <param name="number">The number of free dimension.</param>
    /// <returns>The free dimension with the number <paramref name="number"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Dimension number is out of range.
    /// </exception>
    public Dimension<TIndex> GetFreeDimension(Index number)
    {
        var freeDimensionNumber = GetFreeDimensionNumber(number);
        return AllDimensions[freeDimensionNumber];
    }

    /// <summary>Gets all free dimensions.</summary>
    /// <returns>The collection of all free dimensions.</returns>
    public IReadOnlyList<Dimension<TIndex>> GetFreeDimensions() =>
        Enumerable.Range(0, FreeDimensionCount)
            .Select(i => GetFreeDimension(i))
            .ToList();

    /// <summary>Gets the bound dimension by its number.</summary>
    /// <param name="number">The number of free dimension.</param>
    /// <returns>The free dimension with the number <paramref name="number"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Bound dimension number is out of range.
    /// </exception>
    public Dimension<TIndex> GetBoundDimension(Index number)
    {
        var boundDimensionNumberAndIndex = GetBoundDimensionNumberAndIndex(number);
        return AllDimensions[boundDimensionNumberAndIndex.number];
    }

    /// <summary>Gets the bound index by number of the bound dimension.</summary>
    /// <param name="dimensionNumber">The number of the bound dimension.</param>
    /// <returns>
    /// Gets the bound index by the bound dimension with the number <paramref name="dimensionNumber"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Bound dimension number is out of range.
    /// </exception>
    public TIndex? GetBoundIndex(Index dimensionNumber) =>
        GetBoundDimensionNumberAndIndex(dimensionNumber).index;

    /// <summary>Gets the bound dimension by its number.</summary>
    /// <param name="dimensionNumber">The number of the bound dimension.</param>
    /// <returns>The free dimension with the number <paramref name="dimensionNumber"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Bound dimension number is out of range.
    /// </exception>
    public (Dimension<TIndex> dimension, TIndex? index) GetBoundDimensionAndIndex(
        Index dimensionNumber)
    {
        var (number, index) = GetBoundDimensionNumberAndIndex(dimensionNumber);
        return (AllDimensions[number], index);
    }

    /// <summary>
    /// Gets the array of tuples, containing the bound dimension and
    /// the bound index, associated with this dimension.
    /// </summary>
    /// <returns>
    /// The array of tuples, containing the bound dimension and
    /// the bound index, associated with this dimension.
    /// </returns>
    public (Dimension<TIndex>, TIndex? index)[] GetBoundDimensionsAndIndexes() =>
        BoundDimensionNumbersAndIndexes
            .Select(item => (AllDimensions[item.number], item.index))
            .ToArray();

    /// <summary>
    /// Provides access to cube result as dictionary, in which
    /// key is list of indexes, and value is aggregated value.
    /// </summary>
    /// <returns>
    /// Dictionary, in which key is list of indexes, and value is aggregated value.
    /// </returns>
    public Dictionary<IReadOnlyList<TIndex?>, T> AsDictionary()
    {
        var boundIndexes = BoundDimensionNumbersAndIndexes
            .Select(item => item.number)
            .ToList();
        return ResultMap
            .Where(kvp =>
                kvp.Key.SelectAllAt(boundIndexes)
                    .SequenceEqual(Key.SelectAllAt(boundIndexes)))
            .Select(kvp =>
                kvp.MapKey(key => key.RemoveAllAt(boundIndexes)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private T GetValueFromResultMap(IReadOnlyList<TIndex?> key) =>
        ResultMap.GetValueOrDefault(key.ToEquatableList(), DefaultValue);

    private int GetFreeDimensionNumber(Index dimensionNumber)
    {
        var numberValue = dimensionNumber.GetOffset(FreeDimensionNumbers.Count);

        if (numberValue >= FreeDimensionNumbers.Count)
        {
            throw new ArgumentException($"Dimension number {numberValue} is out of range.", nameof(dimensionNumber));
        }

        return FreeDimensionNumbers[numberValue];
    }

    private (int number, TIndex? index) GetBoundDimensionNumberAndIndex(Index dimensionNumber)
    {
        var dimensionNumberValue = dimensionNumber.GetOffset(BoundDimensionCount);
        if (dimensionNumberValue >= BoundDimensionCount)
        {
            throw new ArgumentException(
                $"Bound dimension number {dimensionNumberValue} is out of range.",
                nameof(dimensionNumber));
        }

        return BoundDimensionNumbersAndIndexes[dimensionNumberValue];
    }
}
