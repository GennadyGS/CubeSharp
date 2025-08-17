using System.Collections;
using System.Globalization;
using Cube.Tests.Data;
using FluentAssertions;
using Xunit;

namespace Cube.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Layout",
    "MEN003:Method is too long",
    Justification = "Test logic is convenient to keep in single method")]
public sealed class CubeBuilderTests
{
    private static readonly string[] StringSourceArray = new[] { "1", "2" };
    private static readonly int[] IntFourSourceArray = new[] { 1, 2, 3, 4 };
    private static readonly int[] IntThreeSourceArray = new[] { 1, 2, 3 };
    private static readonly decimal[] DecimalSourceArray = new[] { 11m, 22m };

    [Fact]
    public void BuildCube_ShouldGenerateCorrect1DCube()
    {
        var cubeResult = TestSourceData.Records.BuildCube(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.A);
        var results =
            new
            {
                Column1 = cubeResult.GetValue("1"),
                Column2 = cubeResult.GetValue("2"),
                Total1and2 = cubeResult.GetValue(Constants.TotalIndex),
                Total = cubeResult.GetValue(),
            };

        results.Should().BeEquivalentTo(
            new
            {
                Column1 = TestSourceData.Records.Where(r => r.A == "1").Sum(r => r.D),
                Column2 = TestSourceData.Records.Where(r => r.A == "2").Sum(r => r.D),
                Total1and2 = TestSourceData.Records.Where(r => StringSourceArray.Contains(r.A)).Sum(r => r.D),
                Total = TestSourceData.Records.Sum(r => r.D),
            });
    }

    [Fact]
    public void BuildCube_ShouldGenerateCorrect1DCubeWithMultiSelector()
    {
        var cubeResult = TestSourceData.Records.BuildCube(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.E);
        var results =
            new
            {
                Column1 = cubeResult.GetValue("1"),
                Column2 = cubeResult.GetValue("2"),
                Column3 = cubeResult.GetValue("3"),
                Column4 = cubeResult.GetValue("4"),
                Column1_4 = cubeResult.GetValue(Constants.TotalIndex),
                Total = cubeResult.GetValue(),
            };

        results.Should().BeEquivalentTo(
            new
            {
                Column1 = TestSourceData.Records.Where(r => r.E.Contains(1)).Sum(r => r.D),
                Column2 = TestSourceData.Records.Where(r => r.E.Contains(2)).Sum(r => r.D),
                Column3 = TestSourceData.Records.Where(r => r.E.Contains(3)).Sum(r => r.D),
                Column4 = TestSourceData.Records.Where(r => r.E.Contains(4)).Sum(r => r.D),
                Column1_4 = TestSourceData.Records
                    .Where(r => r.E.Intersect(IntFourSourceArray).Any()).Sum(r => r.D),
                Total = TestSourceData.Records.Sum(r => r.D),
            });
    }

    [Fact]
    public void BuildCube_ShouldGenerateCorrect1DCubeWithCompositeAggregation()
    {
        var sumOfCAndSumOfD = AggregationDefinition.Create(
            (TestSourceRecord row) => new { row.C, D = row.D ?? 0 },
            (left, right) =>
                new
                {
                    C = left.C + right.C,
                    D = left.D + right.D,
                },
            new { C = 0m, D = 0L });

        var cubeResult = TestSourceData.Records.BuildCube(
            sumOfCAndSumOfD,
            TestDimensionDefinitions.A);
        var results =
            new
            {
                Column1SumOfC = cubeResult.GetValue("1").C,
                Column1SumOfD = cubeResult.GetValue("1").D,
                Column2SumOfC = cubeResult.GetValue("2").C,
                Column2SumOfD = cubeResult.GetValue("2").D,
                Total1and2SumOfC = cubeResult.GetValue(Constants.TotalIndex).C,
                Total1and2SumOfD = cubeResult.GetValue(Constants.TotalIndex).D,
                TotalSumOfC = cubeResult.GetValue().C,
                TotalSumOfD = cubeResult.GetValue().D,
            };

        results.Should().BeEquivalentTo(
            new
            {
                Column1SumOfC = TestSourceData.Records.Where(r => r.A == "1").Sum(r => r.C),
                Column1SumOfD = TestSourceData.Records.Where(r => r.A == "1").Sum(r => r.D),
                Column2SumOfC = TestSourceData.Records.Where(r => r.A == "2").Sum(r => r.C),
                Column2SumOfD = TestSourceData.Records.Where(r => r.A == "2").Sum(r => r.D),
                Total1and2SumOfC = TestSourceData.Records
                    .Where(r => StringSourceArray.Contains(r.A))
                    .Sum(r => r.C),
                Total1and2SumOfD = TestSourceData.Records
                    .Where(r => StringSourceArray.Contains(r.A))
                    .Sum(r => r.D),
                TotalSumOfC = TestSourceData.Records.Sum(r => r.C),
                TotalSumOfD = TestSourceData.Records.Sum(r => r.D),
            });
    }

    [Fact]
    public void BuildCube_ShouldGenerateCorrect2DCube()
    {
        var cubeResult = TestSourceData.Records.BuildCube(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.B,
            TestDimensionDefinitions.A);
        var results = (
                from indexB in TestDimensionDefinitions.B
                select new
                {
                    RowIndex = indexB.Value,
                    IsTotalRow = indexB.Children.Any(),
                    Column1 = cubeResult.GetValue(indexB.Value, "1"),
                    Column2 = cubeResult.GetValue(indexB.Value, "2"),
                    Column1and2 = cubeResult.GetValue(indexB.Value, Constants.TotalIndex),
                    ColumnTotal = cubeResult.GetValue(indexB.Value),
                })
            .ToList();

        results.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    RowIndex = Constants.TotalIndex,
                    IsTotalRow = true,
                    Column1 = TestSourceData.Records
                        .Where(r => IntThreeSourceArray.Contains(r.B) && r.A == "1")
                        .Sum(r => r.D),
                    Column2 = TestSourceData.Records
                        .Where(r => IntThreeSourceArray.Contains(r.B) && r.A == "2")
                        .Sum(r => r.D),
                    Column1and2 = TestSourceData.Records
                        .Where(r => IntThreeSourceArray.Contains(r.B) && StringSourceArray.Contains(r.A))
                        .Sum(r => r.D),
                    ColumnTotal = TestSourceData.Records
                        .Where(r => IntThreeSourceArray.Contains(r.B))
                        .Sum(r => r.D),
                },
            }.Concat(
                IntThreeSourceArray
                    .Select(b =>
                        new
                        {
                            RowIndex = b.ToString(),
                            IsTotalRow = false,
                            Column1 = TestSourceData.Records
                                .Where(r => r.B == b && r.A == "1").Sum(r => r.D),
                            Column2 = TestSourceData.Records
                                .Where(r => r.B == b && r.A == "2").Sum(r => r.D),
                            Column1and2 = TestSourceData.Records
                                .Where(r => r.B == b && StringSourceArray.Contains(r.A))
                                .Sum(r => r.D),
                            ColumnTotal = TestSourceData.Records.Where(r => r.B == b).Sum(r => r.D),
                        })),
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void BuildCube_ShouldGenerateCorrect2DCubeWithMultiSelector()
    {
        var cubeResult = TestSourceData.Records.BuildCube(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.E,
            TestDimensionDefinitions.A);
        var results = (
                from indexE in TestDimensionDefinitions.E
                select new
                {
                    RowIndex = indexE.Value,
                    IsTotalRow = indexE.Children.Any(),
                    Column1 = cubeResult.GetValue(indexE.Value, "1"),
                    Column2 = cubeResult.GetValue(indexE.Value, "2"),
                    Column1and2 = cubeResult.GetValue(indexE.Value, Constants.TotalIndex),
                    ColumnTotal = cubeResult.GetValue(indexE.Value),
                })
            .ToList();

        results.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    RowIndex = Constants.TotalIndex,
                    IsTotalRow = true,
                    Column1 = TestSourceData.Records
                        .Where(r => r.E.Intersect(IntFourSourceArray).Any() && r.A == "1")
                        .Sum(r => r.D),
                    Column2 = TestSourceData.Records
                        .Where(r => r.E.Intersect(IntFourSourceArray).Any() && r.A == "2")
                        .Sum(r => r.D),
                    Column1and2 = TestSourceData.Records
                        .Where(r => r.E.Intersect(IntFourSourceArray).Any() && StringSourceArray.Contains(r.A))
                        .Sum(r => r.D),
                    ColumnTotal = TestSourceData.Records
                        .Where(r => r.E.Intersect(IntFourSourceArray).Any())
                        .Sum(r => r.D),
                },
            }.Concat(
                IntFourSourceArray
                    .Select(e =>
                        new
                        {
                            RowIndex = e.ToString(),
                            IsTotalRow = false,
                            Column1 = TestSourceData.Records
                                .Where(r => r.E.Contains(e) && r.A == "1").Sum(r => r.D),
                            Column2 = TestSourceData.Records
                                .Where(r => r.E.Contains(e) && r.A == "2").Sum(r => r.D),
                            Column1and2 = TestSourceData.Records
                                .Where(r => r.E.Contains(e) && StringSourceArray.Contains(r.A))
                                .Sum(r => r.D),
                            ColumnTotal = TestSourceData.Records.Where(r => r.E.Contains(e)).Sum(r => r.D),
                        })),
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void BuildCube_ShouldGenerateCorrect3DCube()
    {
        var cubeResult = TestSourceData.Records.BuildCube(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.C,
            TestDimensionDefinitions.B,
            TestDimensionDefinitions.A);
        IEnumerable results = (
                from indexC in TestDimensionDefinitions.C
                from indexB in TestDimensionDefinitions.B
                select new
                {
                    RowGroupIndex = indexC.Value,
                    RowIndex = indexB.Value,
                    RowIndexTitle = indexB.Title,
                    Column1 = cubeResult.GetValue(indexC.Value, indexB.Value, "1"),
                    Column2 = cubeResult.GetValue(indexC.Value, indexB.Value, "2"),
                    Column1and2 = cubeResult.GetValue(indexC.Value, indexB.Value, Constants.TotalIndex),
                    ColumnTotal = cubeResult.GetValue(indexC.Value, indexB.Value),
                })
            .ToList();

        results.Should().BeEquivalentTo(
            GetExpectedResult(),
            options => options.WithStrictOrdering());
        return;

        static IEnumerable GetExpectedResult()
        {
            foreach (var rowGroupIndex in new[] { Constants.TotalIndex, "11", "22" })
            {
                foreach (var p in GetExpectedRowGroup(rowGroupIndex))
                {
                    yield return p;
                }
            }
        }

        static IEnumerable GetExpectedRowGroup(string rowGroupIndex)
        {
            foreach (var rowIndex in new[] { Constants.TotalIndex, "1", "2", "3" })
            {
                bool RowGroupMatches(TestSourceRecord r) =>
                    rowGroupIndex == Constants.TotalIndex
                        ? DecimalSourceArray.Contains(r.C)
                        : rowGroupIndex == r.C.ToString(CultureInfo.InvariantCulture);

                bool RowPredicate(TestSourceRecord r) =>
                    RowGroupMatches(r) &&
                    (rowIndex == Constants.TotalIndex
                        ? IntThreeSourceArray.Contains(r.B)
                        : rowIndex == r.B.ToString());

                yield return new
                {
                    RowGroupIndex = rowGroupIndex,
                    RowIndex = rowIndex,
                    Column1 = TestSourceData.Records
                        .Where(r => RowPredicate(r) && r.A == "1")
                        .Sum(r => r.D),
                    Column2 = TestSourceData.Records
                        .Where(r => RowPredicate(r) && r.A == "2")
                        .Sum(r => r.D),
                    Column1and2 = TestSourceData.Records
                        .Where(r => RowPredicate(r) && StringSourceArray.Contains(r.A))
                        .Sum(r => r.D),
                    ColumnTotal = TestSourceData.Records
                        .Where(RowPredicate)
                        .Sum(r => r.D),
                };
            }
        }
    }

    [Fact]
    public async Task BuildCubeAsync_ShouldGenerateCorrect1DCube()
    {
        var cubeResult = await TestSourceData.Records
            .ToAsyncEnumerable()
            .BuildCubeAsync(
                TestAggregationDefinitions.SumOfD,
                TestDimensionDefinitions.A);
        var results =
            new
            {
                Column1 = cubeResult.GetValue("1"),
                Column2 = cubeResult.GetValue("2"),
                Total1and2 = cubeResult.GetValue(Constants.TotalIndex),
                Total = cubeResult.GetValue(),
            };

        results.Should().BeEquivalentTo(
            new
            {
                Column1 = TestSourceData.Records.Where(r => r.A == "1").Sum(r => r.D),
                Column2 = TestSourceData.Records.Where(r => r.A == "2").Sum(r => r.D),
                Total1and2 = TestSourceData.Records.Where(r => StringSourceArray.Contains(r.A)).Sum(r => r.D),
                Total = TestSourceData.Records.Sum(r => r.D),
            });
    }

    [Fact]
    public async Task BuildCubeAsync_ShouldGenerateCorrect1DCubeWithMultiSelector()
    {
        var cubeResult = await TestSourceData.Records.ToAsyncEnumerable().BuildCubeAsync(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.E);
        var results =
            new
            {
                Column1 = cubeResult.GetValue("1"),
                Column2 = cubeResult.GetValue("2"),
                Column3 = cubeResult.GetValue("3"),
                Column4 = cubeResult.GetValue("4"),
                Column1_4 = cubeResult.GetValue(Constants.TotalIndex),
                Total = cubeResult.GetValue(),
            };

        results.Should().BeEquivalentTo(
            new
            {
                Column1 = TestSourceData.Records.Where(r => r.E.Contains(1)).Sum(r => r.D),
                Column2 = TestSourceData.Records.Where(r => r.E.Contains(2)).Sum(r => r.D),
                Column3 = TestSourceData.Records.Where(r => r.E.Contains(3)).Sum(r => r.D),
                Column4 = TestSourceData.Records.Where(r => r.E.Contains(4)).Sum(r => r.D),
                Column1_4 = TestSourceData.Records
                    .Where(r => r.E.Intersect(IntFourSourceArray).Any()).Sum(r => r.D),
                Total = TestSourceData.Records.Sum(r => r.D),
            });
    }

    [Fact]
    public async Task BuildCubeAsync_ShouldGenerateCorrect1DCubeWithCompositeAggregation()
    {
        var sumOfCAndSumOfD = AggregationDefinition.Create(
            (TestSourceRecord row) => new { row.C, D = row.D ?? 0 },
            (left, right) =>
                new
                {
                    C = left.C + right.C,
                    D = left.D + right.D,
                },
            new { C = 0m, D = 0L });

        var cubeResult = await TestSourceData.Records.ToAsyncEnumerable().BuildCubeAsync(
            sumOfCAndSumOfD,
            TestDimensionDefinitions.A);
        var results =
            new
            {
                Column1SumOfC = cubeResult.GetValue("1").C,
                Column1SumOfD = cubeResult.GetValue("1").D,
                Column2SumOfC = cubeResult.GetValue("2").C,
                Column2SumOfD = cubeResult.GetValue("2").D,
                Total1and2SumOfC = cubeResult.GetValue(Constants.TotalIndex).C,
                Total1and2SumOfD = cubeResult.GetValue(Constants.TotalIndex).D,
                TotalSumOfC = cubeResult.GetValue().C,
                TotalSumOfD = cubeResult.GetValue().D,
            };

        results.Should().BeEquivalentTo(
            new
            {
                Column1SumOfC = TestSourceData.Records.Where(r => r.A == "1").Sum(r => r.C),
                Column1SumOfD = TestSourceData.Records.Where(r => r.A == "1").Sum(r => r.D),
                Column2SumOfC = TestSourceData.Records.Where(r => r.A == "2").Sum(r => r.C),
                Column2SumOfD = TestSourceData.Records.Where(r => r.A == "2").Sum(r => r.D),
                Total1and2SumOfC = TestSourceData.Records
                    .Where(r => StringSourceArray.Contains(r.A))
                    .Sum(r => r.C),
                Total1and2SumOfD = TestSourceData.Records
                    .Where(r => StringSourceArray.Contains(r.A))
                    .Sum(r => r.D),
                TotalSumOfC = TestSourceData.Records.Sum(r => r.C),
                TotalSumOfD = TestSourceData.Records.Sum(r => r.D),
            });
    }

    [Fact]
    public async Task BuildCubeAsync_ShouldGenerateCorrect2DCube()
    {
        var cubeResult = await TestSourceData.Records.ToAsyncEnumerable().BuildCubeAsync(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.B,
            TestDimensionDefinitions.A);
        var results = (
                from indexB in TestDimensionDefinitions.B
                select new
                {
                    RowIndex = indexB.Value,
                    IsTotalRow = indexB.Children.Any(),
                    Column1 = cubeResult.GetValue(indexB.Value, "1"),
                    Column2 = cubeResult.GetValue(indexB.Value, "2"),
                    Column1and2 = cubeResult.GetValue(indexB.Value, Constants.TotalIndex),
                    ColumnTotal = cubeResult.GetValue(indexB.Value),
                })
            .ToList();

        results.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    RowIndex = Constants.TotalIndex,
                    IsTotalRow = true,
                    Column1 = TestSourceData.Records
                        .Where(r => IntThreeSourceArray.Contains(r.B) && r.A == "1")
                        .Sum(r => r.D),
                    Column2 = TestSourceData.Records
                        .Where(r => IntThreeSourceArray.Contains(r.B) && r.A == "2")
                        .Sum(r => r.D),
                    Column1and2 = TestSourceData.Records
                        .Where(r => IntThreeSourceArray.Contains(r.B) && StringSourceArray.Contains(r.A))
                        .Sum(r => r.D),
                    ColumnTotal = TestSourceData.Records
                        .Where(r => IntThreeSourceArray.Contains(r.B))
                        .Sum(r => r.D),
                },
            }.Concat(
                IntThreeSourceArray
                    .Select(b =>
                        new
                        {
                            RowIndex = b.ToString(),
                            IsTotalRow = false,
                            Column1 = TestSourceData.Records
                                .Where(r => r.B == b && r.A == "1").Sum(r => r.D),
                            Column2 = TestSourceData.Records
                                .Where(r => r.B == b && r.A == "2").Sum(r => r.D),
                            Column1and2 = TestSourceData.Records
                                .Where(r => r.B == b && StringSourceArray.Contains(r.A))
                                .Sum(r => r.D),
                            ColumnTotal = TestSourceData.Records.Where(r => r.B == b).Sum(r => r.D),
                        })),
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task BuildCubeAsync_ShouldGenerateCorrect2DCubeWithMultiSelector()
    {
        var cubeResult = await TestSourceData.Records.ToAsyncEnumerable().BuildCubeAsync(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.E,
            TestDimensionDefinitions.A);
        var results = (
                from indexE in TestDimensionDefinitions.E
                select new
                {
                    RowIndex = indexE.Value,
                    IsTotalRow = indexE.Children.Any(),
                    Column1 = cubeResult.GetValue(indexE.Value, "1"),
                    Column2 = cubeResult.GetValue(indexE.Value, "2"),
                    Column1and2 = cubeResult.GetValue(indexE.Value, Constants.TotalIndex),
                    ColumnTotal = cubeResult.GetValue(indexE.Value),
                })
            .ToList();

        results.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    RowIndex = Constants.TotalIndex,
                    IsTotalRow = true,
                    Column1 = TestSourceData.Records
                        .Where(r => r.E.Intersect(IntFourSourceArray).Any() && r.A == "1")
                        .Sum(r => r.D),
                    Column2 = TestSourceData.Records
                        .Where(r => r.E.Intersect(IntFourSourceArray).Any() && r.A == "2")
                        .Sum(r => r.D),
                    Column1and2 = TestSourceData.Records
                        .Where(r => r.E.Intersect(IntFourSourceArray).Any() && StringSourceArray.Contains(r.A))
                        .Sum(r => r.D),
                    ColumnTotal = TestSourceData.Records
                        .Where(r => r.E.Intersect(IntFourSourceArray).Any())
                        .Sum(r => r.D),
                },
            }.Concat(
                IntFourSourceArray
                    .Select(e =>
                        new
                        {
                            RowIndex = e.ToString(),
                            IsTotalRow = false,
                            Column1 = TestSourceData.Records
                                .Where(r => r.E.Contains(e) && r.A == "1").Sum(r => r.D),
                            Column2 = TestSourceData.Records
                                .Where(r => r.E.Contains(e) && r.A == "2").Sum(r => r.D),
                            Column1and2 = TestSourceData.Records
                                .Where(r => r.E.Contains(e) && StringSourceArray.Contains(r.A))
                                .Sum(r => r.D),
                            ColumnTotal = TestSourceData.Records.Where(r => r.E.Contains(e)).Sum(r => r.D),
                        })),
            options => options.WithStrictOrdering());
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Minor Code Smell",
        "S3776:Cognitive Complexity of methods should not be too high",
        Justification = "This method is required due to legacy constraints.")]
    [Fact]
    public async Task BuildCubeAsync_ShouldGenerateCorrect3DCube()
    {
        IEnumerable GetExpectedResult()
        {
            foreach (var rowGroupIndex in new[] { Constants.TotalIndex, "11", "22" })
            {
                bool RowGroupMatches(TestSourceRecord r) =>
                    rowGroupIndex == Constants.TotalIndex
                        ? DecimalSourceArray.Contains(r.C)
                        : rowGroupIndex == r.C.ToString(CultureInfo.InvariantCulture);

                foreach (var rowIndex in new[] { Constants.TotalIndex, "1", "2", "3" })
                {
                    bool RowPredicate(TestSourceRecord r) =>
                        RowGroupMatches(r) &&
                        (rowIndex == Constants.TotalIndex
                            ? IntThreeSourceArray.Contains(r.B)
                            : rowIndex == r.B.ToString());

                    yield return new
                    {
                        RowGroupIndex = rowGroupIndex,
                        RowIndex = rowIndex,
                        Column1 = TestSourceData.Records
                            .Where(r => RowPredicate(r) && r.A == "1")
                            .Sum(r => r.D),
                        Column2 = TestSourceData.Records
                            .Where(r => RowPredicate(r) && r.A == "2")
                            .Sum(r => r.D),
                        Column1and2 = TestSourceData.Records
                            .Where(r => RowPredicate(r) && StringSourceArray.Contains(r.A))
                            .Sum(r => r.D),
                        ColumnTotal = TestSourceData.Records
                            .Where(RowPredicate)
                            .Sum(r => r.D),
                    };
                }
            }
        }

        var cubeResult = await TestSourceData.Records.ToAsyncEnumerable().BuildCubeAsync(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.C,
            TestDimensionDefinitions.B,
            TestDimensionDefinitions.A);
        IEnumerable results = (
                from indexC in TestDimensionDefinitions.C
                from indexB in TestDimensionDefinitions.B
                select new
                {
                    RowGroupIndex = indexC.Value,
                    RowIndex = indexB.Value,
                    RowIndexTitle = indexB.Title,
                    Column1 = cubeResult.GetValue(indexC.Value, indexB.Value, "1"),
                    Column2 = cubeResult.GetValue(indexC.Value, indexB.Value, "2"),
                    Column1and2 = cubeResult.GetValue(indexC.Value, indexB.Value, Constants.TotalIndex),
                    ColumnTotal = cubeResult.GetValue(indexC.Value, indexB.Value),
                })
            .ToList();

        results.Should().BeEquivalentTo(
            GetExpectedResult(),
            options => options.WithStrictOrdering());
    }
}
