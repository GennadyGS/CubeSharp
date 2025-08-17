using Cube.Utils;

namespace Cube;

/// <summary>
/// Provides static methods for generating data cubes.
/// </summary>
public static class CubeBuilder
{
    /// <summary>
    /// Builds the cube and returns the instance of <seealso cref="CubeResult{TIndex,T}"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the source sequence's item.</typeparam>
    /// <typeparam name="TIndex">The type of the index.</typeparam>
    /// <typeparam name="T">The type of the aggregation result.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="aggregationDefinition">The aggregation definition.</param>
    /// <param name="dimensionDefinitions">The dimension definitions.</param>
    /// <returns>The instance of <seealso cref="CubeResult{TIndex,T}"/>.</returns>
    public static CubeResult<TIndex, T> BuildCube<TSource, TIndex, T>(
        this IEnumerable<TSource> source,
        AggregationDefinition<TSource, T> aggregationDefinition,
        params DimensionDefinition<TSource, TIndex>[] dimensionDefinitions)
    {
        var resultMap = source
            .SelectMany(
                record => GetAffectedKeys(record, dimensionDefinitions),
                (record, key) => (record, key))
            .ToDictionaryWithAggregation(
                item => item.key,
                item => aggregationDefinition.SelectValue(item.record),
                aggregationDefinition.AggregationFunction);
        return new CubeResult<TIndex, T>(
            resultMap, aggregationDefinition.SeedValue, dimensionDefinitions);
    }

    /// <summary>
    /// Builds the cube from asynchronous sequence and returns the task of
    /// <seealso cref="CubeResult{TIndex,T}"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the source sequence's item.</typeparam>
    /// <typeparam name="TIndex">The type of the index.</typeparam>
    /// <typeparam name="T">The type of the aggregation result.</typeparam>
    /// <param name="source">The source asynchronous sequence.</param>
    /// <param name="aggregationDefinition">The aggregation definition.</param>
    /// <param name="dimensionDefinitions">The dimension definitions.</param>
    /// <returns>The task of <seealso cref="CubeResult{TIndex,T}"/>.</returns>
    public static async Task<CubeResult<TIndex, T>> BuildCubeAsync<TSource, TIndex, T>(
        this IAsyncEnumerable<TSource> source,
        AggregationDefinition<TSource, T> aggregationDefinition,
        params DimensionDefinition<TSource, TIndex>[] dimensionDefinitions)
    {
        var resultMap = await source
            .SelectManyAsync(
                record => GetAffectedKeys(record, dimensionDefinitions),
                (record, key) => (record, key))
            .ToDictionaryWithAggregationAsync(
                item => item.key,
                item => aggregationDefinition.SelectValue(item.record),
                aggregationDefinition.AggregationFunction);
        return new CubeResult<TIndex, T>(
            resultMap, aggregationDefinition.SeedValue, dimensionDefinitions);
    }

    private static IEnumerable<IReadOnlyList<TIndex>> GetAffectedKeys<TSource, TIndex>(
        TSource record,
        DimensionDefinition<TSource, TIndex>[] dimensionDefinitions) =>
        dimensionDefinitions
            .Select(def => def.GetAffectedIndexes(record).ToArray())
            .ToArray()
            .GetAllCombinations()
            .Select(items => items.ToEquatableList());
}
