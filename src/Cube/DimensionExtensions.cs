namespace Cube;

/// <summary>
/// Provides static methods for additional operations on the instances of
/// <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
/// </summary>
public static class DimensionExtensions
{
    /// <summary>
    /// Sets the title of the instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which dimension is defined.
    /// </typeparam>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="dimension">
    /// The target instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="title">The title.</param>
    /// <returns>
    /// The modified instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// with title <paramref name="title"/>.
    /// </returns>
    public static DimensionDefinition<TSource, TIndex> WithTitle<TSource, TIndex>(
        this DimensionDefinition<TSource, TIndex> dimension,
        string title)
    {
        dimension.Title = title;
        return dimension;
    }

    /// <summary>
    /// Adds the metadata entry to the instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which dimension is defined.
    /// </typeparam>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="dimension">
    /// The target instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="key">The key of the metadata entry.</param>
    /// <param name="value">The value of the metadata entry.</param>
    /// <returns>
    /// The modified instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// with added metadata entry with key <paramref name="key"/>
    /// and value <paramref name="value"/>.
    /// </returns>
    public static DimensionDefinition<TSource, TIndex> WithMetadata<TSource, TIndex>(
        this DimensionDefinition<TSource, TIndex> dimension,
        string key,
        object value)
    {
        dimension.AddMetadata(key, value);
        return dimension;
    }

    /// <summary>
    /// Adds the multiple metadata entries to the instance of
    /// <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which dimension is defined.
    /// </typeparam>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="dimension">
    /// The target instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="metadata">
    /// The array of tuples of key and value specifying the metadata entries.
    /// </param>
    /// <returns>
    /// The modified instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// with added metadata entries <paramref name="metadata"/>.
    /// </returns>
    public static DimensionDefinition<TSource, TIndex> WithMetadata<TSource, TIndex>(
        this DimensionDefinition<TSource, TIndex> dimension,
        params (string key, object value)[] metadata)
    {
        dimension.AddMetadata(metadata);
        return dimension;
    }

    /// <summary>
    /// Creates the new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// with additional leading default index.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which dimension is defined.
    /// </typeparam>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="dimension">
    /// The target instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="title">The title for default index.</param>
    /// <returns>
    /// The new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// with additional leading root default index.
    /// </returns>
    /// <exception cref="ArgumentException">Dimension already contains default index.</exception>
    public static DimensionDefinition<TSource, TIndex> WithLeadingDefaultIndex<TSource, TIndex>(
        this DimensionDefinition<TSource, TIndex> dimension,
        string title = default)
    {
        if (dimension.ContainsIndex(default))
        {
            throw new ArgumentException("Dimension already contains default index.");
        }

        return dimension.WithIndexDefinitions(
            IndexDefinition.Create(default, title, dimension.IndexDefinitions.ToArray()));
    }

    /// <summary>
    /// Creates the new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// with additional trailing default index.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which dimension is defined.
    /// </typeparam>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="dimension">
    /// The target instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="title">The title for default index.</param>
    /// <returns>
    /// The new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// with additional trailing root default index.
    /// </returns>
    /// <exception cref="ArgumentException">Dimension already contains default index.</exception>
    public static DimensionDefinition<TSource, TIndex> WithTrailingDefaultIndex<TSource, TIndex>(
        this DimensionDefinition<TSource, TIndex> dimension,
        string title = default)
    {
        if (dimension.ContainsIndex(default))
        {
            throw new ArgumentException("Dimension already contains default index.");
        }

        return dimension.WithIndexDefinitions(
            IndexDefinition.Create(dimension.IndexDefinitions.ToArray(), default, title));
    }

    /// <summary>
    /// Gets the parent index value for specified index value inside dimension.
    /// </summary>
    /// <param name="dimension">
    /// The target instance of <seealso cref="Dimension{TIndex}"/>.
    /// </param>
    /// <param name="index">The index value.</param>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <returns>
    /// The parent index value for index value <paramref name="index"/>.
    /// </returns>
    /// <remarks>
    /// If index value is not found or does not have parents, <c>default</c>
    /// will be returned.
    /// </remarks>
    public static TIndex GetParentIndex<TIndex>(
        this Dimension<TIndex> dimension,
        TIndex index) =>
        dimension.GetAffectedIndexes(index)
            .Skip(1)
            .FirstOrDefault();
}
