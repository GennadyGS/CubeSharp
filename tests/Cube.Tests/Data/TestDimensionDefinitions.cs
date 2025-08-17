using System.Globalization;

namespace Cube.Tests.Data;

internal static class TestDimensionDefinitions
{
    public static readonly DimensionDefinition<TestSourceRecord, string> A =
        DimensionDefinition.Create(
            (TestSourceRecord record) => record.A,
            null,
            IndexDefinition.Create(
                Constants.TotalIndex,
                "A Total",
                IndexDefinition.Create("1", "A 1"),
                IndexDefinition.Create("2", "A 2")));

    public static readonly DimensionDefinition<TestSourceRecord, string> B =
        DimensionDefinition.Create(
            (TestSourceRecord record) => record.B.ToString(),
            null,
            IndexDefinition.Create(
                Constants.TotalIndex,
                "B Total",
                IndexDefinition.Create("1", "B 1"),
                IndexDefinition.Create("2", "B 2"),
                IndexDefinition.Create("3", "B 3")));

    public static readonly DimensionDefinition<TestSourceRecord, string> C =
        DimensionDefinition.Create(
            (TestSourceRecord record) => record.C.ToString(CultureInfo.InvariantCulture),
            null,
            IndexDefinition.Create(
                Constants.TotalIndex,
                "C Total",
                IndexDefinition.Create("11", "C 11"),
                IndexDefinition.Create("22", "C 22")));

    public static readonly DimensionDefinition<TestSourceRecord, string> E =
        DimensionDefinition.CreateWithMultiSelector(
            (TestSourceRecord record) => record.E.Select(x => x.ToString(CultureInfo.InvariantCulture)),
            null,
            IndexDefinition.Create(
                Constants.TotalIndex,
                "E Total",
                IndexDefinition.Create("1", "E 1"),
                IndexDefinition.Create("2", "E 2"),
                IndexDefinition.Create("3", "E 3"),
                IndexDefinition.Create("4", "E 4")));
}
