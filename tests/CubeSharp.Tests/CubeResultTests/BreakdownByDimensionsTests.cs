using CubeSharp.Tests.Data;
using FluentAssertions;
using Xunit;

namespace CubeSharp.Tests.CubeResultTests;

public sealed class BreakdownByDimensionsTests
{
    public BreakdownByDimensionsTests()
    {
        Sut = TestSourceData.Records.BuildCube(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.A,
            TestDimensionDefinitions.B);
    }

    private CubeResult<string, long> Sut { get; }

    [Fact]
    public void BreakdownByDimensions_ThrowsException_WhenContainsDuplicatesOfDimensionNumbers()
    {
        Action action = () => Sut.BreakdownByDimensions(1, 1);

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0, 1, 3)]
    [InlineData(1, 2, 4)]
    [InlineData(0, 2, 12)]
    public void BreakdownByDimensions_ReturnsCollectionOfCorrectSize(
        int startOfRange,
        int endOfRange,
        int expectedCollectionSize)
    {
        var indexesOfDimension = Sut.BreakdownByDimensions(startOfRange..endOfRange);

        indexesOfDimension.Count().Should().Be(expectedCollectionSize);
    }

    [Fact]
    public void BreakdownByDimensions_ThrowsException_WhenDimensionOutOfRange()
    {
        Action action = () => Sut.BreakdownByDimensions(2);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BreakdownByDimensions_ReturnsCorrectResult()
    {
        var expectedSumOfD = TestSourceData.Records
            .Where(r => r.A == "1" || r.A == "2")
            .GroupBy(r => r.A)
            .Select(groups => groups.Sum(r => r.D)).ToList();

        expectedSumOfD.Add(expectedSumOfD.Sum());   // Add total to resulting list

        var sumOfDBySlices = Sut.BreakdownByDimensions(0)
            .Select(s => s.GetValue()).ToList();

        sumOfDBySlices.Should().BeEquivalentTo(expectedSumOfD);
    }
}
