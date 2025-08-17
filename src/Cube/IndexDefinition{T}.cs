using Cube.Utils;

namespace Cube;

/// <summary>Represents the cube dimension index definition.</summary>
/// <typeparam name="T">The type of the index value.</typeparam>
public sealed class IndexDefinition<T> : DefinitionBase
{
    internal IndexDefinition(
        T value,
        string title,
        IReadOnlyCollection<IndexDefinition<T>> children,
        bool parentAfterChildren)
        : base(title)
    {
        Value = value;
        Children = children;
        ParentAfterChildren = parentAfterChildren;

        if (Children.Any(def => def.IsDefault))
        {
            throw new ArgumentException("Default index cannot be nested.", nameof(children));
        }

        if (GetChildrenRecursive().HasDuplicatesBy(def => def.Value))
        {
            throw new ArgumentException("Index definition contains duplicated values.", nameof(children));
        }
    }

    /// <summary>Gets the value of the index.</summary>
    /// <value>The value of the index.</value>
    public T Value { get; }

    /// <summary>Gets the collection of the child index definitions.</summary>
    /// <value>The collection of the child index definitions.</value>
    public IReadOnlyCollection<IndexDefinition<T>> Children { get; }

    /// <summary>
    /// Gets a value indicating whether the index definition is default.
    /// </summary>
    /// <value>
    /// <c>true</c> if the index definition is default; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// Default index is playing the role of total index for dimension
    /// and can only be the root index in dimension.
    /// </remarks>
    public bool IsDefault =>
        EqualityComparer<T>.Default.Equals(Value, default);

    private bool ParentAfterChildren { get; }

    internal IReadOnlyCollection<IndexDefinition<T>> GetChildrenRecursive()
    {
        var childIndexes = Children.SelectMany(def => def.GetChildrenRecursive());
        return ParentAfterChildren
            ? childIndexes.ConcatItem(this).ToList()
            : childIndexes.ConcatToItem(this).ToList();
    }
}
