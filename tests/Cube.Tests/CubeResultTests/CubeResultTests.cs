using Cube.Tests.Data;
using FluentAssertions;
using Xunit;

namespace Cube.Tests.CubeResultTests;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Layout",
    "MEN003:Method is too long",
    Justification = "Test logic is convenient to keep in single method")]
public sealed class CubeResultTests
{
    public CubeResultTests()
    {
        Sut = TestSourceData.Records.BuildCube(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.A,
            TestDimensionDefinitions.B);
    }

    private CubeResult<string, long> Sut { get; }

    [Fact]
    public void AsDictionary_ShouldReturnCorrectResult()
    {
        bool AInDimension(string? value) =>
            TestDimensionDefinitions.A.ContainsIndex(value);

        bool BInDimension(int value) =>
            TestDimensionDefinitions.B.ContainsIndex(value.ToString());

        IEnumerable<KeyValuePair<string?[], long?>> GetEntriesGroupedBy(
            Func<TestSourceRecord, bool> predicate,
            Func<TestSourceRecord, string?> getA,
            Func<TestSourceRecord, string?> getB) =>
            TestSourceData.Records
                .Where(predicate)
                .GroupBy(r => (A: getA(r), B: getB(r)))
                .Select(g =>
                    KeyValuePair.Create(
                        new[] { g.Key.A, g.Key.B },
                        g.Sum(r => r.D)));

        var result = Sut.AsDictionary();

        var expectedResult =
            GetEntriesGroupedBy(
                    r => AInDimension(r.A) && BInDimension(r.B),
                    r => r.A,
                    r => r.B.ToString())
                .Concat(
                    GetEntriesGroupedBy(
                        r => AInDimension(r.A) && BInDimension(r.B),
                        r => r.A,
                        r => Constants.TotalIndex))
                .Concat(
                    GetEntriesGroupedBy(
                        r => AInDimension(r.A),
                        r => r.A,
                        r => default))
                .Concat(
                    GetEntriesGroupedBy(
                        r => AInDimension(r.A) && BInDimension(r.B),
                        r => Constants.TotalIndex,
                        r => r.B.ToString()))
                .Concat(
                    GetEntriesGroupedBy(
                        r => AInDimension(r.A) && BInDimension(r.B),
                        r => Constants.TotalIndex,
                        r => Constants.TotalIndex))
                .Concat(
                    GetEntriesGroupedBy(
                        r => AInDimension(r.A),
                        r => Constants.TotalIndex,
                        r => default))
                .Concat(
                    GetEntriesGroupedBy(
                        r => BInDimension(r.B),
                        r => default,
                        r => r.B.ToString()))
                .Concat(
                    GetEntriesGroupedBy(
                        r => BInDimension(r.B),
                        r => default,
                        r => Constants.TotalIndex))
                .Concat(
                    GetEntriesGroupedBy(
                        r => true,
                        r => default,
                        r => default))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        result.AsEnumerable().Should().BeEquivalentTo(expectedResult.AsEnumerable());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData(default)]
    public void AsDictionary_ShouldReturnCorrectResult_WhenCubeIsSliced(string indexA)
    {
        bool BInDimension(int value) =>
            TestDimensionDefinitions.B.ContainsIndex(value.ToString());

        IEnumerable<KeyValuePair<string?[], long?>> GetEntriesGroupedBy(
            Func<TestSourceRecord, bool> predicate,
            Func<TestSourceRecord, string?> getB) =>
            TestSourceData.Records
                .Where(r => (indexA == default || r.A == indexA) && predicate(r))
                .GroupBy(getB)
                .Select(g =>
                    KeyValuePair.Create(
                        new[] { g.Key },
                        g.Sum(r => r.D)));

        var result = Sut[indexA].AsDictionary().ToList();

        var expectedResult =
            GetEntriesGroupedBy(
                    r => BInDimension(r.B),
                    r => r.B.ToString())
                .Concat(
                    GetEntriesGroupedBy(
                        r => BInDimension(r.B),
                        r => Constants.TotalIndex))
                .Concat(
                    GetEntriesGroupedBy(
                        r => true,
                        r => default))
                .ToDictionary(kvp => kvp.Key.ToList(), kvp => kvp.Value)
                .ToList();
        result
            .Should()
            .BeEquivalentTo(expectedResult, opts => opts.WithAutoConversion());
    }

    [Fact]
    public void GetBoundDimensionNumberAndIndex_ThrowsArgumentException_WhenDimensionNumberIsOutOfRange()
    {
        Action action = () => Sut.GetBoundDimensionAndIndex(0);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetBoundDimension_ReturnsCorrectResult_WhenCubeIsSlicedAndNumberIsFirst()
    {
        var slice = Sut["1"];

        var result = slice.GetBoundDimension(0);

        result.Should().BeEquivalentTo(TestDimensionDefinitions.A);
    }

    [Fact]
    public void GetBoundDimension_ReturnsCorrectResult_WhenCubeIsSlicedAndNumberIsLast()
    {
        var slice = Sut["1"];

        var result = slice.GetBoundDimension(^1);

        result.Should().BeEquivalentTo(TestDimensionDefinitions.A);
    }

    [Fact]
    public void GetBoundDimension_ReturnsCorrectResult_WhenCubeIsSlicedBySecondDimensionAndNumberIsFirst()
    {
        var slice = Sut.Slice(1, "3");

        var result = slice.GetBoundDimension(0);

        result.Should().BeEquivalentTo(TestDimensionDefinitions.B);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("")]
    [InlineData(default)]
    public void GetBoundIndex_ReturnsCorrectResult_WhenCubeIsSlicedAndNumberIsFirst(string index)
    {
        var slice = Sut[index];

        var result = slice.GetBoundIndex(0);

        result.Should().BeEquivalentTo(index);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("")]
    [InlineData(default)]
    public void GetBoundIndex_ReturnsCorrectResult_WhenCubeIsSlicedAndNumberIsLast(string index)
    {
        var slice = Sut[index];

        var result = slice.GetBoundIndex(^1);

        result.Should().BeEquivalentTo(index);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("")]
    [InlineData(default)]
    public void GetBoundIndex_ReturnsCorrectResult_WhenCubeIsSlicedBySecondDimensionAndNumberIsFirst(string index)
    {
        var slice = Sut.Slice(1, index);

        var result = slice.GetBoundIndex(0);

        result.Should().BeEquivalentTo(index);
    }

    [Fact]
    public void FreeDimensionCount_ShouldReturnCorrectValue_WithoutSlicing()
    {
        Sut.FreeDimensionCount.Should().Be(2);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("No index", 1)]
    public void FreeDimensionCount_ShouldReturnCorrectValue_AfterSlicing(string indexValue, int expectedFreeDimensions)
    {
        var slicedCube = Sut.Slice(indexValue);

        slicedCube.FreeDimensionCount.Should().Be(expectedFreeDimensions);
    }

    [Theory]
    [InlineData("1", "2", 0)]
    [InlineData("2", "1", 0)]
    [InlineData("1", "No index", 0)]
    [InlineData("No index", "1", 0)]
    public void FreeDimensionCount_ShouldReturnCorrectValue_AfterSlicingByTwoDimensions(
        string firstIndexValue,
        string secondIndexValue,
        int expectedFreeDimensions)
    {
        var slicedCube = Sut.Slice(firstIndexValue, secondIndexValue);

        slicedCube.FreeDimensionCount.Should().Be(expectedFreeDimensions);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("No index")]
    public void GetBoundDimensionsAndIndexes_ShouldReturnCorrectValue_AfterSlicing(string indexValue)
    {
        var slicedCube = Sut.Slice(indexValue);

        var res = slicedCube.GetBoundDimensionsAndIndexes();

        res.Length.Should().Be(1);
        res.FirstOrDefault().index.Should().Be(indexValue);
    }

    [Theory]
    [InlineData("1", "A 1")]
    [InlineData("2", "A 2")]
    public void GetBoundIndexDefinition_ReturnsCorrectIndexDefinition_AfterSlicing(
        string indexValue,
        string expectedTitle)
    {
        var slicedCube = Sut.Slice(indexValue);

        var indexDefinition = slicedCube.GetBoundIndexDefinition(0);

        indexDefinition.Value.Should().Be(indexValue);
        indexDefinition.Title.Should().Be(expectedTitle);
    }
}
