namespace CubeSharp;

/// <summary>
/// Provides static method for creating the instances of <seealso cref="IndexDefinition{T}"/>.
/// </summary>
public static class IndexDefinition
{
    /// <summary>
    /// Creates the instance of <seealso cref="IndexDefinition{T}"/>
    /// with default order of index definitions.
    /// </summary>
    /// <typeparam name="T">The type of the index value.</typeparam>
    /// <param name="value">The value of the index definition.</param>
    /// <param name="title">The title of the index definition (optional).</param>
    /// <param name="children">The collection of the child index definitions.</param>
    /// <returns>The new instance of <seealso cref="IndexDefinition{T}"/>.</returns>
    /// <remarks>
    /// Child index definitions <paramref name="children"/> will appear after the parent
    /// on enumerating the dimension indexes. In order to put child indexes on top use
    /// overloaded method <seealso cref="Create{T}(IndexDefinition{T}[], T, string)"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Default index cannot be nested.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Index definition contains duplicated values.
    /// </exception>
    public static IndexDefinition<T> Create<T>(
        T? value,
        string? title = null,
        params IndexDefinition<T>[] children) =>
        new IndexDefinition<T>(value, title, children, false);

    /// <summary>
    /// Creates the instance of <seealso cref="IndexDefinition{T}"/> with child index
    /// definitions before parent on enumerating the dimension indexes.
    /// </summary>
    /// <typeparam name="T">The type of the index value.</typeparam>
    /// <param name="children">The collection of the child index definitions.</param>
    /// <param name="value">The value of the index definition.</param>
    /// <param name="title">The title of the index definition (optional).</param>
    /// <returns>The new instance of <seealso cref="IndexDefinition{T}"/>.</returns>
    /// <remarks>
    /// Child index definitions <paramref name="children"/> will appear before the parent
    /// on enumerating the dimension indexes. In order to put child indexes on bottom use
    /// overloaded method <seealso cref="Create{T}(T,string,IndexDefinition{T}[])"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Default index cannot be nested.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Index definition contains duplicated values.
    /// </exception>
    public static IndexDefinition<T> Create<T>(
        IndexDefinition<T>[] children, T? value, string? title = null)
        where T : notnull =>
        new IndexDefinition<T>(value, title, children, true);
}
