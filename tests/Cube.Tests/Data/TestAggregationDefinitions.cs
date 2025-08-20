using Cube;

namespace CubeSharp.Tests.Data;

internal static class TestAggregationDefinitions
{
    public static readonly AggregationDefinition<TestSourceRecord, long> SumOfD =
        AggregationDefinition.Create(
            (TestSourceRecord record) => record.D ?? 0,
            (x, y) => x + y,
            0L);
}
