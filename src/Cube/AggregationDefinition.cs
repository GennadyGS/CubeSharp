using System.Linq.Expressions;

namespace Cube;

/// <summary>
/// Provides static method for creating the instances of <seealso cref="AggregationDefinition{TSource,T}"/>.
/// </summary>
public static class AggregationDefinition
{
    /// <summary>
    /// Creates the instance of <seealso cref="AggregationDefinition{TSource,T}"/>.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which aggregation is defined.
    /// </typeparam>
    /// <typeparam name="T">The type of the aggregation result.</typeparam>
    /// <param name="valueSelector">
    /// The value selector function, used for retrieving the target
    /// sequence of type <typeparamref name="T"/> for the aggregation from the
    /// source sequence of type <typeparamref name="TSource"/>.
    /// </param>
    /// <param name="aggregationFunction">
    /// The aggregation function to be applied to target sequence of type <typeparamref name="T"/>.
    /// </param>
    /// <param name="seedValue">The initial value for the aggregation.</param>
    /// <returns>
    /// The new instance of <seealso cref="AggregationDefinition{TSource,T}"/>.
    /// </returns>
    public static AggregationDefinition<TSource, T> Create<TSource, T>(
        Expression<Func<TSource, T>> valueSelector,
        Func<T, T, T> aggregationFunction,
        T seedValue) =>
        new AggregationDefinition<TSource, T>(
            valueSelector,
            aggregationFunction,
            seedValue);

    /// <summary>
    /// Creates the instance of <seealso cref="AggregationDefinition{TSource,T}"/>
    /// provided that source data type is <see cref="IDictionary{TKey,TValue}"/>
    /// of <seealso cref="string"/> and <see cref="object"/>.
    /// </summary>
    /// <typeparam name="T">The type of the aggregation result.</typeparam>
    /// <param name="valueSelector">
    /// The value selector function, used for retrieving the target
    /// sequence of type <typeparamref name="T"/> for the aggregation from the
    /// source sequence of type <see cref="IDictionary{TKey,TValue}"/>
    /// of <seealso cref="string"/> and <see cref="object"/>.
    /// </param>
    /// <param name="aggregationFunction">
    /// The aggregation function to be applied to target sequence of type <typeparamref name="T"/>.
    /// </param>
    /// <param name="seedValue">The initial value for the aggregation.</param>
    /// <returns>
    /// The new instance of <seealso cref="AggregationDefinition{TSource,T}"/>.
    /// </returns>
    public static AggregationDefinition<IDictionary<string, object>, T> CreateForDictionaryCollection<T>(
        Expression<Func<IDictionary<string, object>, T>> valueSelector,
        Func<T, T, T> aggregationFunction,
        T seedValue) =>
        Create(valueSelector, aggregationFunction, seedValue);

    /// <summary>
    /// Creates the instance of <seealso cref="AggregationDefinition{TSource,T}"/>
    /// provided that source data type should be inferred from parameter <paramref name="collection"/>.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source data type for which aggregation is defined.
    /// </typeparam>
    /// <typeparam name="T">The type of the aggregation result.</typeparam>
    /// <param name="collection">The collection source data type
    /// <typeparamref name="TSource"/> intended only for the type inference.
    /// </param>
    /// <param name="valueSelector">
    /// The value selector function, used for retrieving the target
    /// sequence of type <typeparamref name="T"/> for the aggregation from the
    /// source sequence of type <typeparamref name="TSource"/>.
    /// </param>
    /// <param name="aggregationFunction">
    /// The aggregation function to be applied to target sequence of type <typeparamref name="T"/>.
    /// </param>
    /// <param name="seedValue">The initial value for the aggregation.</param>
    /// <returns>
    /// The new instance of <seealso cref="AggregationDefinition{TSource,T}"/>.
    /// </returns>
    /// <remarks>
    /// This method is necessary then type of source collection
    /// <typeparamref name="TSource"/> is anonymous type.
    /// </remarks>
    public static AggregationDefinition<TSource, T> CreateForCollection<TSource, T>(
        IEnumerable<TSource> collection,
        Expression<Func<TSource, T>> valueSelector,
        Func<T, T, T> aggregationFunction,
        T seedValue) =>
        Create(
            valueSelector,
            aggregationFunction,
            seedValue);
}
