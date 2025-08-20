using System.Linq.Expressions;

namespace CubeSharp;

/// <summary>Represents the cube dimension definition.</summary>
/// <typeparam name="TSource">
/// The source data type for which dimension is defined.
/// </typeparam>
/// <typeparam name="TIndex">The type of the dimension index.</typeparam>
public sealed class DimensionDefinition<TSource, TIndex> : Dimension<TIndex>
    where TIndex : notnull
{
    internal DimensionDefinition(
        Expression<Func<TSource, IEnumerable<TIndex?>>> indexSelector,
        string? title,
        params IndexDefinition<TIndex>[] indexDefinitions)
        : this(indexSelector, indexSelector.Compile(), title, indexDefinitions)
    {
        IndexSelector = indexSelector;
        IndexSelectorFunc = indexSelector.Compile();
    }

    private DimensionDefinition(
        Expression<Func<TSource, IEnumerable<TIndex?>>> indexSelector,
        Func<TSource, IEnumerable<TIndex?>> indexSelectorFunc,
        string? title,
        params IndexDefinition<TIndex>[] indexDefinitions)
        : base(title, indexDefinitions)
    {
        IndexSelector = indexSelector;
        IndexSelectorFunc = indexSelectorFunc;
    }

    /// <summary>Gets the index selector function.</summary>
    /// <value>The index selector function.</value>
    /// <remarks>
    /// The function to map the element of source collection to the index value.
    /// </remarks>
    public Expression<Func<TSource, IEnumerable<TIndex?>>> IndexSelector { get; }

    private Func<TSource, IEnumerable<TIndex?>> IndexSelectorFunc { get; }

    /// <summary>
    /// Selects the list of indexes by applying <seealso cref="IndexSelector"/>
    /// to source collection <paramref name="source"></paramref>.
    /// </summary>
    /// <param name="source">The source collection.</param>
    /// <returns>
    /// The list of indexes which are returned from <seealso cref="IndexSelector"/>
    /// applied to source collection.
    /// </returns>
    public IEnumerable<TIndex?> SelectIndexes(TSource source) =>
        IndexSelectorFunc.Invoke(source);

    internal IEnumerable<TIndex?> GetAffectedIndexes(TSource source) =>
        GetPrimaryIndexes(source)
            .SelectMany(GetAffectedIndexes)
            .Distinct()
            .DefaultIfEmpty();

    internal IEnumerable<TIndex?> GetPrimaryIndexes(TSource source) =>
        SelectIndexes(source).Select(GetPrimaryIndex);

    internal DimensionDefinition<TSource, TIndex> WithIndexDefinitions(
        params IndexDefinition<TIndex>[] indexDefinitions) =>
        new DimensionDefinition<TSource, TIndex>(
            IndexSelector, IndexSelectorFunc, Title, indexDefinitions);
}
