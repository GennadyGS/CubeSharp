using Cube.Utils;

namespace Cube;

/// <summary>
/// Provides static methods for additional operations on the instances of
/// <seealso cref="IndexDefinition{T}"/>.
/// </summary>
public static class IndexDimensionExtensions
{
    /// <summary>
    /// Sets the title of the instance of <seealso cref="IndexDefinition{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the index value.</typeparam>
    /// <param name="index">
    /// The target instance of <seealso cref="IndexDefinition{T}"/>.
    /// </param>
    /// <param name="title">The title.</param>
    /// <returns>
    /// The modified instance of <seealso cref="IndexDefinition{T}"/>
    /// with title <paramref name="title"/>.
    /// </returns>
    public static IndexDefinition<T> WithTitle<T>(
        this IndexDefinition<T> index,
        string title)
    {
        index.Title = title;
        return index;
    }

    /// <summary>
    /// Adds the metadata entry to the instance of <seealso cref="IndexDefinition{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the index value.</typeparam>
    /// <param name="index">
    /// The target instance of <seealso cref="IndexDefinition{T}"/>.
    /// </param>
    /// <param name="key">The key of the metadata entry.</param>
    /// <param name="value">The value of the metadata entry.</param>
    /// <returns>
    /// The modified instance of <seealso cref="IndexDefinition{T}"/>
    /// with added metadata entry with key <paramref name="key"/>
    /// and value <paramref name="value"/>.
    /// </returns>
    public static IndexDefinition<T> WithMetadata<T>(
        this IndexDefinition<T> index,
        string key,
        object value)
    {
        index.AddMetadata(key, value);
        return index;
    }

    /// <summary>
    /// Adds the multiple metadata entries to the instance of <seealso cref="IndexDefinition{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the index value.</typeparam>
    /// <param name="index">
    /// The target instance of <seealso cref="IndexDefinition{T}"/>.
    /// </param>
    /// <param name="metadata">
    /// The array of tuples of key and value specifying the metadata entries.
    /// </param>
    /// <returns>
    /// The modified instance of <seealso cref="IndexDefinition{T}"/>
    /// with added metadata entries <paramref name="metadata"/>.
    /// </returns>
    public static IndexDefinition<T> WithMetadata<T>(
        this IndexDefinition<T> index,
        params (string key, object value)[] metadata)
    {
        index.AddMetadata(metadata);
        return index;
    }

    internal static (T? value, IReadOnlyList<T?> path)[] GetIndexPaths<T>(
        this IndexDefinition<T> indexDefinition, IReadOnlyCollection<T?> parentPath)
    {
        var path = parentPath.ConcatToItem(indexDefinition.Value).ToArray();
        return indexDefinition.Children
            .SelectMany(child => GetIndexPaths(child, path))
            .ConcatToItem((value: indexDefinition.Value, path))
            .ToArray();
    }
}
