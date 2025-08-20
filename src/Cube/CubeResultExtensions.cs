using Cube.Utils;

namespace Cube;

/// <summary>
/// Provides extension methods for <see cref="CubeResult{TIndex,T}"/>.
/// </summary>
public static class CubeResultExtensions
{
    /// <summary>
    /// Slices the <paramref name="cubeResult"/> by the index <paramref name="index"/>
    /// in the first free dimension.
    /// </summary>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <typeparam name="T">The type of the aggregated value.</typeparam>
    /// <param name="cubeResult">
    /// The source instance of the <seealso cref="CubeResult{TIndex,T}"/>.
    /// </param>
    /// <param name="index">
    /// The index in the first free dimension
    /// by which to slice the <see cref="CubeResult{TIndex, T}"/>.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="CubeResult{TIndex, T}"/> sliced
    /// by the index <paramref name="index"/> in the first free dimension.
    /// </returns>
    public static CubeResult<TIndex, T> Slice<TIndex, T>(
        this CubeResult<TIndex, T> cubeResult, TIndex index)
        where TIndex : notnull =>
        cubeResult.Slice(0, index);

    /// <summary>
    /// Slices the <paramref name="cubeResult"/> by the indexes
    /// <paramref name="indexes"/> specified in order of free dimensions.
    /// </summary>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <typeparam name="T">The type of the aggregated value.</typeparam>
    /// <param name="cubeResult">
    /// The source instance of the <seealso cref="CubeResult{TIndex,T}"/>.
    /// </param>
    /// <param name="indexes">
    /// The indexes specified in order of free dimensions
    /// by which to slice the <see cref="CubeResult{TIndex, T}"/>.
    /// </param>
    /// <returns>
    /// A new instance of the <see cref="CubeResult{TIndex, T}"/> sliced by
    /// the indexes <paramref name="indexes"/> specified in order of free dimensions.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Dimension number is out of range.
    /// </exception>
    public static CubeResult<TIndex, T> Slice<TIndex, T>(
        this CubeResult<TIndex, T> cubeResult, params TIndex?[] indexes)
        where TIndex : notnull =>
        cubeResult.Slice(
            indexes
                .Select((index, dimensionNumber) => ((Index)dimensionNumber, index))
                .ToArray());

    /// <summary>
    /// Breaks down the <paramref name="cubeResult"/> by the free dimensions
    /// with the numbers in the range <paramref name="dimensionNumbersRange"/>.
    /// </summary>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <typeparam name="T">The type of the aggregated value.</typeparam>
    /// <param name="cubeResult">
    /// The source instance of the <seealso cref="CubeResult{TIndex,T}"/>.
    /// </param>
    /// <param name="dimensionNumbersRange">
    /// The range of the numbers of the free dimensions
    /// by which to break down the <paramref name="cubeResult"/>.
    /// </param>
    /// <returns>
    /// A collection of <see cref="CubeResult{TIndex, T}"/> instances, each broken down by
    /// the free dimensions with the numbers in the range <paramref name="dimensionNumbersRange"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Dimension number is out of range.
    /// </exception>
    public static IEnumerable<CubeResult<TIndex, T>> BreakdownByDimensions<TIndex, T>(
        this CubeResult<TIndex, T> cubeResult, Range dimensionNumbersRange)
        where TIndex : notnull
    {
        var (offset, length) = dimensionNumbersRange.GetOffsetAndLength(cubeResult.FreeDimensionCount);
        return cubeResult.BreakdownByDimensions(
            Enumerable.Range(offset, length).ToArray());
    }

    /// <summary>
    /// Breaks down the <paramref name="cubeResult"/> by the free dimensions
    /// with the numbers <paramref name="dimensionNumbers"/>.
    /// </summary>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <typeparam name="T">The type of the aggregated value.</typeparam>
    /// <param name="cubeResult">
    /// The source instance of the <seealso cref="CubeResult{TIndex,T}"/>.
    /// </param>
    /// <param name="dimensionNumbers">
    /// The numbers of the free dimensions by which to break down the <paramref name="cubeResult"/>.
    /// </param>
    /// <returns>
    /// A collection of <see cref="CubeResult{TIndex, T}"/> instances, each broken down by
    /// the free dimensions with the numbers <paramref name="dimensionNumbers"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Dimension number is out of range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Dimension numbers should not contain duplicates.
    /// </exception>
    public static IEnumerable<CubeResult<TIndex, T>> BreakdownByDimensions<TIndex, T>(
        this CubeResult<TIndex, T> cubeResult, params Index[] dimensionNumbers)
        where TIndex : notnull =>
        cubeResult.BreakdownByDimensions(
            dimensionNumbers
                .Select(index => index.GetOffset(cubeResult.FreeDimensionCount))
                .ToArray());

    /// <summary>
    /// Gets the definition of the index in the bound dimension
    /// with the number <paramref name="dimensionNumber"/>.
    /// </summary>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <typeparam name="T">The type of the aggregated value.</typeparam>
    /// <param name="cubeResult">
    /// The source instance of the <seealso cref="CubeResult{TIndex,T}"/>.
    /// </param>
    /// <param name="dimensionNumber">The number of the bound dimension.</param>
    /// <returns>
    /// The definition of the index in the bound dimension
    /// with the number <paramref name="dimensionNumber"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Bound dimension number is out of range.
    /// </exception>
    public static IndexDefinition<TIndex> GetBoundIndexDefinition<TIndex, T>(
        this CubeResult<TIndex, T> cubeResult, Index dimensionNumber)
        where TIndex : notnull
    {
        var (dimension, index) = cubeResult.GetBoundDimensionAndIndex(dimensionNumber);
        return dimension[index];
    }

    private static IEnumerable<CubeResult<TIndex, T>> BreakdownByDimensions<TIndex, T>(
        this CubeResult<TIndex, T> cubeResult,
        params int[] dimensionNumbers)
        where TIndex : notnull
    {
        if (dimensionNumbers.HasDuplicatesBy(x => x))
        {
            throw new ArgumentException("Dimension numbers should not contain duplicates.");
        }

        return dimensionNumbers
            .Select(number => cubeResult
                .GetFreeDimension(number)
                .Select(indexDef => ((Index)number, indexDef.Value))
                .ToArray())
            .ToArray()
            .GetAllCombinations()
            .Select(key => cubeResult.Slice(key.ToArray()));
    }
}
