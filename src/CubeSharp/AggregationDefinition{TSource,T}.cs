using System.Linq.Expressions;

namespace CubeSharp;

/// <summary>Represents the aggregation definition.</summary>
/// <typeparam name="TSource">
/// The source data type for which aggregation is defined.
/// </typeparam>
/// <typeparam name="T">The type of the aggregation result.</typeparam>
/// <remarks>
/// Aggregation function is applied over a sequence of target values of type
/// <typeparamref name="T"/>, which is retrieved from source sequence of type
/// <typeparamref name="TSource"/> by applying the value selector function.
/// The specified seed value is used as the initial accumulator value.
/// </remarks>
public sealed class AggregationDefinition<TSource, T>
{
    internal AggregationDefinition(
        Expression<Func<TSource, T>> valueSelector,
        Func<T, T, T> aggregationFunction,
        T seedValue)
    {
        ValueSelector = valueSelector;
        ValueSelectorFunc = valueSelector.Compile();
        AggregationFunction = aggregationFunction;
        SeedValue = seedValue;
    }

    /// <summary>Gets the value selector function.</summary>
    /// <value>The value selector function.</value>
    /// <remarks>
    /// The value selector function is used for retrieving the target
    /// sequence of type <typeparamref name="T"/> for the aggregation from the
    /// source sequence of type <typeparamref name="TSource"/>.
    /// </remarks>
    public Expression<Func<TSource, T>> ValueSelector { get; }

    /// <summary>Gets the aggregation function.</summary>
    /// <value>
    /// The aggregation function to be applied to target sequence of type <typeparamref name="T"/>.
    /// </value>
    public Func<T, T, T> AggregationFunction { get; }

    /// <summary>Gets the initial value for the aggregation.</summary>
    /// <value>The initial value for the aggregation.</value>
    public T SeedValue { get; }

    private Func<TSource, T> ValueSelectorFunc { get; }

    /// <summary>
    /// Selects the target value for the aggregation from the instance
    /// of the source data type <typeparamref name="TSource"/>.
    /// </summary>
    /// <param name="source">
    /// The instance of the source data type <typeparamref name="TSource"/>.
    /// </param>
    /// <returns>
    /// The target value for the aggregation.
    /// </returns>
    /// <remarks>
    /// It is used for retrieving the target
    /// sequence of type <typeparamref name="T"/> for the aggregation from the
    /// source sequence of type <typeparamref name="TSource"/>.
    /// </remarks>
    public T SelectValue(TSource source) =>
        ValueSelectorFunc.Invoke(source);
}
