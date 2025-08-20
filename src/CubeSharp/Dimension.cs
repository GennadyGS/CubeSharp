using System.Collections;
using Cube.Utils;

namespace Cube;

/// <summary>Represents the cube dimension.</summary>
/// <typeparam name="TIndex">The type of the dimension index.</typeparam>
public class Dimension<TIndex>
    : DefinitionBase, IEnumerable<IndexDefinition<TIndex>>
    where TIndex : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Dimension{TIndex}"/> class.
    /// </summary>
    /// <param name="title">The title of the dimension.</param>
    /// <param name="indexDefinitions">The collection of the top-level index definitions.</param>
    /// <remarks>
    /// The constructor builds internal maps for fast lookup and validates that if the default
    /// (root) index is present it is the only root index defined.
    /// </remarks>
    protected Dimension(
        string? title,
        IReadOnlyCollection<IndexDefinition<TIndex>> indexDefinitions)
        : base(title)
    {
        IndexDefinitions = indexDefinitions;
        IndexMap = GetIndexMap();
        AffectedIndexesMap = GetAffectedIndexesMap();

        if (ContainsIndex(default)
            && indexDefinitions.Count > 1)
        {
            throw new ArgumentException("Default index should be the only root index.");
        }
    }

    /// <summary>Gets the collection of the index definitions.</summary>
    /// <value>The collection of the top-level index definitions.</value>
    public IReadOnlyCollection<IndexDefinition<TIndex>> IndexDefinitions { get; }

    private IReadOnlyDictionary<TIndex?, IndexDefinition<TIndex>> IndexMap { get; }

    private IReadOnlyDictionary<TIndex?, IReadOnlyList<TIndex?>> AffectedIndexesMap { get; }

    /// <summary>Retrieves the index definition by its value.</summary>
    /// <param name="index">The value of the index to locate.</param>
    /// <returns>The desired index definition.</returns>
    /// <remarks>Searches by index definition hierarchy recursively.</remarks>
    /// <exception cref="ArgumentException">Index is not found in dimension.</exception>
    public IndexDefinition<TIndex> this[TIndex? index] =>
        IndexMap.TryGetValue(index, out var result)
            ? result
            : throw new ArgumentException($"Index {index} is not found in dimension", nameof(index));

    /// <summary>
    /// Determines whether the dimension contains an index by the specified value.
    /// </summary>
    /// <param name="index">The value of the index to locate.</param>
    /// <returns>
    /// <c>true</c> if the dimension contains an index; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsIndex(TIndex? index) =>
        IndexMap.ContainsKey(index);

    /// <summary>
    /// Returns an enumerator that iterates through the collection of
    /// all index definitions recursively.
    /// </summary>
    /// <returns>
    /// An <seealso cref="IEnumerator"/> object that can be used to iterate
    /// through the collection of all index definitions recursively.
    /// </returns>
    public IEnumerator<IndexDefinition<TIndex>> GetEnumerator() =>
        GetIndexDefinitionsRecursive().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal IEnumerable<TIndex?> GetAffectedIndexes(TIndex? primaryIndex) =>
        AffectedIndexesMap.GetValueOrDefault(
            primaryIndex,
            new[] { default(TIndex) });

    internal TIndex? GetPrimaryIndex(TIndex? index) =>
        ContainsIndex(index)
            ? index
            : default;

    private IReadOnlyDictionary<TIndex?, IndexDefinition<TIndex>> GetIndexMap()
    {
        var indexDefinitionsRecursive = GetIndexDefinitionsRecursive();
        if (indexDefinitionsRecursive.HasDuplicatesBy(def => def.Value))
        {
            throw new ArgumentException("Index definitions contain duplicates");
        }

        return indexDefinitionsRecursive.ToDictionarySupportingNullKeys(def => def.Value);
    }

    private IReadOnlyCollection<IndexDefinition<TIndex>> GetIndexDefinitionsRecursive() =>
        IndexDefinitions
            .SelectMany(indexDefinition => indexDefinition.GetChildrenRecursive())
            .ToList();

    private IReadOnlyDictionary<TIndex?, IReadOnlyList<TIndex?>> GetAffectedIndexesMap()
    {
        IReadOnlyCollection<TIndex?> GetRootPath(IndexDefinition<TIndex> index) =>
            index.IsDefault
                ? Array.Empty<TIndex?>()
                : new[] { (TIndex?)default };

        return IndexDefinitions
            .SelectMany(index => index.GetIndexPaths(GetRootPath(index)))
            .ToDictionarySupportingNullKeys(item => item.value, item => item.path);
    }
}
