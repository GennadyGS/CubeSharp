using System.Linq.Expressions;

namespace Cube.Utils;

internal static class ExpressionExtensions
{
    public static Expression<Func<TSource, IEnumerable<TResult>>>
        MapResultToCollection<TSource, TResult>(
            this Expression<Func<TSource, TResult>> expression)
    {
        var sourceParameter = Expression.Parameter(typeof(TSource));
        var body = Expression.NewArrayInit(
            typeof(TResult),
            Expression.Invoke(expression, sourceParameter));
        return Expression.Lambda<Func<TSource, IEnumerable<TResult>>>(body, sourceParameter);
    }
}
