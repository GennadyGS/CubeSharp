using System.Linq.Expressions;
using CubeSharp.Utils;

namespace CubeSharp;

/// <summary>
/// Provides static method for creating the instances of
/// <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
/// </summary>
public static class DimensionDefinition
{
    /// <summary>
    /// Creates the instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// for arbitrary source data type.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which dimension is defined.
    /// </typeparam>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="indexSelector">
    /// The function to map the element of source collection to the index value.
    /// </param>
    /// <param name="title">
    /// The title of the creating instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="indexDefinitions">
    /// The collection of the index definitions of the creating instance
    /// of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <returns>
    /// The new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Index definitions contain duplicates.</exception>
    /// <exception cref="ArgumentException">Default index should be the only root index.</exception>
    public static DimensionDefinition<TSource, TIndex> Create<TSource, TIndex>(
        Expression<Func<TSource, TIndex?>> indexSelector,
        string? title = null,
        params IndexDefinition<TIndex>[] indexDefinitions)
        where TIndex : notnull =>
        new(indexSelector.MapResultToCollection(), title, indexDefinitions);

    /// <summary>
    /// Creates the instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// for arbitrary source data type. Accepts the <paramref name="indexSelector"/>
    /// selecting multiple index values.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which dimension is defined.
    /// </typeparam>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="indexSelector">
    /// The function to map the element of source collection to the collection of index values.
    /// </param>
    /// <param name="title">
    /// The title of the creating instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="indexDefinitions">
    /// The collection of the index definitions of the creating instance
    /// of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <returns>
    /// The new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Index definitions contain duplicates.</exception>
    /// <exception cref="ArgumentException">Default index should be the only root index.</exception>
    public static DimensionDefinition<TSource, TIndex> CreateWithMultiSelector<TSource, TIndex>(
        Expression<Func<TSource, IEnumerable<TIndex?>>> indexSelector,
        string? title = null,
        params IndexDefinition<TIndex>[] indexDefinitions)
        where TIndex : notnull =>
        new(indexSelector, title, indexDefinitions);

    /// <summary>
    /// Creates the instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// for arbitrary source data type with the single default index value.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which dimension is defined.
    /// </typeparam>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="title">
    /// The title of the creating instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="indexTitle">
    /// The title of the single default index definition.
    /// </param>
    /// <returns>
    /// The new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// with the single default index value.
    /// </returns>
    /// <remarks>
    /// Default dimensions can be used for calculating only total value for dimension
    /// or as unit dimension e.g. serving as placeholder.
    /// </remarks>
    public static DimensionDefinition<TSource, TIndex> CreateDefault<TSource, TIndex>(
        string? title = null,
        string? indexTitle = null)
        where TIndex : notnull =>
        Create<TSource, TIndex>(
            _ => default,
            title,
            IndexDefinition.Create((TIndex?)default, indexTitle));

    /// <summary>
    /// Creates the instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// provided that source data type is <see cref="IDictionary{TKey,TValue}"/>
    /// of <seealso cref="string"/> and <see cref="object"/>.
    /// </summary>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="indexSelector">
    /// The function to map the element of source collection to the index value.
    /// </param>
    /// <param name="title">
    /// The title of the creating instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="indexDefinitions">
    /// The collection of the index definitions of the creating instance
    /// of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <returns>
    /// The new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Index definitions contain duplicates.</exception>
    /// <exception cref="ArgumentException">Default index should be the only root index.</exception>
    public static DimensionDefinition<IDictionary<string, object?>, TIndex> CreateForDictionaryCollection<TIndex>(
        Expression<Func<IDictionary<string, object?>, TIndex?>> indexSelector,
        string? title = null,
        params IndexDefinition<TIndex>[] indexDefinitions)
        where TIndex : notnull =>
        Create(indexSelector, title, indexDefinitions);

    /// <summary>
    /// Creates the instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// provided that source data type is <see cref="IDictionary{TKey,TValue}"/>.
    /// Accepts the <paramref name="indexSelector"/> selecting multiple index values./>
    /// of <seealso cref="string"/> and <see cref="object"/>.
    /// </summary>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="indexSelector">
    /// The function to map the element of source collection to the collection of index values.
    /// </param>
    /// <param name="title">
    /// The title of the creating instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="indexDefinitions">
    /// The collection of the index definitions of the creating instance
    /// of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <returns>
    /// The new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Index definitions contain duplicates.</exception>
    /// <exception cref="ArgumentException">Default index should be the only root index.</exception>
    public static DimensionDefinition<IDictionary<string, object>, TIndex>
        CreateForDictionaryCollectionWithMultiSelector<TIndex>(
            Expression<Func<IDictionary<string, object>, IEnumerable<TIndex?>>> indexSelector,
            string? title = null,
            params IndexDefinition<TIndex>[] indexDefinitions)
        where TIndex : notnull =>
        CreateWithMultiSelector(indexSelector, title, indexDefinitions);

    /// <summary>
    /// Creates the instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// with the single default index value
    /// provided that source data type is <see cref="IDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="title">
    /// The title of the creating instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="indexTitle">
    /// The title of the single default index definition.
    /// </param>
    /// <returns>
    /// The new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// with the single default index value.
    /// </returns>
    /// <remarks>
    /// Default dimensions can be used for calculating only total value for dimension
    /// or as unit dimension e.g. serving as placeholder.
    /// </remarks>
    public static DimensionDefinition<IDictionary<string, object?>, TIndex>
        CreateDefaultForDictionaryCollection<TIndex>(
            string? title = null,
            string? indexTitle = null)
            where TIndex : notnull =>
        CreateDefault<IDictionary<string, object?>, TIndex>(title, indexTitle);

    /// <summary>
    /// Creates the instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// provided that source data type should be inferred from parameter <paramref name="collection"/>.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which dimension is defined.
    /// </typeparam>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="collection">The collection source data type
    /// <typeparamref name="TSource"/> intended only for the type inference.
    /// </param>
    /// <param name="indexSelector">
    /// The function to map the element of source collection to the index value.
    /// </param>
    /// <param name="title">
    /// The title of the creating instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="indexDefinitions">
    /// The collection of the index definitions of the creating instance
    /// of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <returns>
    /// The new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </returns>
    /// <remarks>
    /// This method is necessary then type of source collection
    /// <typeparamref name="TSource"/> is anonymous type.
    /// </remarks>
    /// <exception cref="ArgumentException">Index definitions contain duplicates.</exception>
    /// <exception cref="ArgumentException">Default index should be the only root index.</exception>
    public static DimensionDefinition<TSource, TIndex> CreateForCollection<TSource, TIndex>(
        IEnumerable<TSource> collection,
        Expression<Func<TSource, TIndex?>> indexSelector,
        string? title = null,
        params IndexDefinition<TIndex>[] indexDefinitions)
        where TIndex : notnull =>
        Create(indexSelector, title, indexDefinitions);

    /// <summary>
    /// Creates the instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>
    /// provided that source data type should be inferred from parameter <paramref name="collection"/>.
    /// Accepts the <paramref name="indexSelector"/> selecting multiple index values.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which dimension is defined.
    /// </typeparam>
    /// <typeparam name="TIndex">The type of the dimension index.</typeparam>
    /// <param name="collection">The collection source data type
    /// <typeparamref name="TSource"/> intended only for the type inference.
    /// </param>
    /// <param name="indexSelector">
    /// The function to map the element of source collection to the collection of index values.
    /// </param>
    /// <param name="title">
    /// The title of the creating instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <param name="indexDefinitions">
    /// The collection of the index definitions of the creating instance
    /// of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </param>
    /// <returns>
    /// The new instance of <seealso cref="DimensionDefinition{TSource,TIndex}"/>.
    /// </returns>
    /// <remarks>
    /// This method is necessary then type of source collection
    /// <typeparamref name="TSource"/> is anonymous type.
    /// </remarks>
    /// <exception cref="ArgumentException">Index definitions contain duplicates.</exception>
    /// <exception cref="ArgumentException">Default index should be the only root index.</exception>
    public static DimensionDefinition<TSource, TIndex>
        CreateForCollectionWithMultiSelector<TSource, TIndex>(
            IEnumerable<TSource> collection,
            Expression<Func<TSource, IEnumerable<TIndex?>>> indexSelector,
            string? title = null,
            params IndexDefinition<TIndex>[] indexDefinitions)
            where TIndex : notnull =>
            CreateWithMultiSelector(indexSelector, title, indexDefinitions);
}
