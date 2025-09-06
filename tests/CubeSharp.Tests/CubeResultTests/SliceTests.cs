using CubeSharp.Tests.Data;
using FluentAssertions;
using Xunit;

namespace CubeSharp.Tests.CubeResultTests;

public sealed class SliceTests
{
    private CubeResult<string, long> Sut { get; } =
        TestSourceData.Records.BuildCube(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.A,
            TestDimensionDefinitions.B);

    [Fact]
    public void Index_ShouldReturnCubeResultOfSameType()
    {
        var result = Sut[null];

        result.Should().BeOfType(Sut.GetType());
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1", "2")]
    [InlineData("1", "3")]
    [InlineData("2", "1")]
    [InlineData("2", "2")]
    [InlineData("2", "3")]
    public void Index_ByIndexInDefinitionShouldReturnCubeResultWithOneDimensionLess(
        string index1, string index2)
    {
        var newCubeResult = Sut[index1];

        var result = newCubeResult.GetValue(index2);

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index1 && r.B.ToString() == index2)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData(" ", "1")]
    [InlineData("3", "2")]
    [InlineData("4", "3")]
    public void Index_ByIndexNotInDefinitionShouldReturnCubeResultWithOneDimensionLess(
        string index1, string index2)
    {
        var newCubeResult = Sut[index1];

        var result = newCubeResult.GetValue(index2);

        result.Should().Be(TestAggregationDefinitions.SumOfD.SeedValue);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    public void Index_ByDefaultShouldReturnCubeResultWithOneDimensionLess(string index2)
    {
        var newCubeResult = Sut[null];

        var result = newCubeResult.GetValue(index2);

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.B.ToString() == index2)
                .Sum(r => r.D));
    }

    [Fact]
    public void DoubleIndex_ShouldReturnCubeResultOfSameType()
    {
        var result = Sut[null][null];

        result.Should().BeOfType(Sut.GetType());
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1", "2")]
    [InlineData("1", "3")]
    [InlineData("2", "1")]
    [InlineData("2", "2")]
    [InlineData("2", "3")]
    public void DoubleIndex_ByIndexesInDefinitionShouldReturnCubeResultWithOneDimensionLess(
        string index1, string index2)
    {
        var newCubeResult = Sut[index1][index2];

        var result = newCubeResult.GetValue();

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index1 && r.B.ToString() == index2)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    public void DoubleIndex_ShouldReturnTotalByFirstDimension_WhenFirstIndexIsDefault(
        string index2)
    {
        var newCubeResult = Sut[null][index2];

        var result = newCubeResult.GetValue();

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.B.ToString() == index2)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    public void DoubleIndex_ShouldReturnTotalBySecondDimension_WhenSecondIndexIsDefault(
        string index1)
    {
        var newCubeResult = Sut[index1][null];

        var result = newCubeResult.GetValue();

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index1)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("1", "1", "1")]
    [InlineData(null, "1", "1")]
    [InlineData("1", "1", null)]
    [InlineData(null, null, null)]
    public void DoubleIndexAndGetValueWithOneArgument_ShouldThrowInvalidOperationException(
        string index1, string index2, string index3)
    {
        var newCubeResult = Sut[index1][index2];

        Action action = () => newCubeResult.GetValue(index3);

        action.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("1", "1", "1")]
    [InlineData(null, "1", "1")]
    [InlineData("1", "1", null)]
    [InlineData(null, null, null)]
    public void TripleIndex_ShouldThrowInvalidOperationException(
        string index1, string index2, string index3)
    {
        Func<CubeResult<string, long>> action =
            () => Sut[index1][index2][index3];

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData(null)]
    public void Slice_ReturnsSliceWithCorrectTotalValue_WhenOneIndexIsSpecified(string indexValue)
    {
        var expectedSumOfD = TestSourceData.Records
            .Where(r => r.A == indexValue || indexValue == null)
            .Sum(r => r.D);

        var slicedCube = Sut.Slice(indexValue);
        var sumOfD = slicedCube.GetValue();

        sumOfD.Should().Be(expectedSumOfD);
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1", "2")]
    [InlineData("1", "3")]
    [InlineData("2", "1")]
    [InlineData("2", "2")]
    [InlineData("2", "3")]
    public void Slice_ByIndexInDefinitionShouldReturnCubeResultWithOneDimensionLess(
        string index1, string index2)
    {
        var newCubeResult = Sut.Slice(index1);

        var result = newCubeResult.GetValue(index2);

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index1 && r.B.ToString() == index2)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1", "2")]
    [InlineData("1", "3")]
    [InlineData(null, "3")]
    [InlineData("2", "1")]
    [InlineData("2", "2")]
    [InlineData("2", "3")]
    [InlineData("2", null)]
    public void Slice_ReturnsSliceWithCorrectTotalValue_WhenTwoIndexesAreSpecified(
        string firstIndexValue,
        string secondIndexValue)
    {
        var expectedSumOfD = TestSourceData.Records
            .Where(r => (r.A == firstIndexValue || firstIndexValue == null)
                        && (r.B.ToString() == secondIndexValue || secondIndexValue == null))
            .Sum(r => r.D);

        var slicedCube = Sut.Slice(firstIndexValue, secondIndexValue);
        var sumOfD = slicedCube.GetValue();

        sumOfD.Should().Be(expectedSumOfD);
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1", "2")]
    [InlineData("1", "3")]
    [InlineData("2", "1")]
    [InlineData("2", "2")]
    [InlineData("2", "3")]
    public void SliceByDimensionNumber_ShouldReturnCubeResult_WhenIndexIsInDimension(
        string index1, string index2)
    {
        var newCubeResult = Sut.Slice(1, index2);

        var result = newCubeResult.GetValue(index1);

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index1 && r.B.ToString() == index2)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    public void SliceByDimensionNumberFromEnd_ShouldReturnCubeResult_WhenIndexIsInDimension(
        string index)
    {
        var newCubeResult = Sut.Slice(^1, index);

        var result = newCubeResult.GetValue();

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.B.ToString() == index)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1", "2")]
    [InlineData("1", "3")]
    [InlineData("2", "1")]
    [InlineData("2", "2")]
    [InlineData("2", "3")]
    public void DoubleSliceByDimensionNumber_ShouldReturnCubeResult_WhenIndexesAreInDimensions(
        string index1, string index2)
    {
        var newCubeResult = Sut.Slice(0, index1).Slice(0, index2);

        var result = newCubeResult.GetValue();

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index1 && r.B.ToString() == index2)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1", "2")]
    [InlineData("1", "3")]
    [InlineData("2", "1")]
    [InlineData("2", "2")]
    [InlineData("2", "3")]
    public void DoubleSliceByDimensionNumber_ShouldReturnCubeResult_WhenIndexesAreInDimensionsAndOrderIsReverse(
        string index1, string index2)
    {
        var newCubeResult = Sut.Slice((1, index2), (0, index1));

        var result = newCubeResult.GetValue();

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index1 && r.B.ToString() == index2)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1", "2")]
    [InlineData("1", "3")]
    [InlineData("2", "1")]
    [InlineData("2", "2")]
    [InlineData("2", "3")]
    public void Slice_ShouldReturnCubeResultWithOneDimensionLess_WhenIndexesAreInDimensionsAndOrderIsReverse(
        string index1, string index2)
    {
        var newCubeResult = Sut.Slice(1, index2).Slice(0, index1);

        var result = newCubeResult.GetValue();

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index1 && r.B.ToString() == index2)
                .Sum(r => r.D));
    }

    [Fact]
    public void Slice_ShouldThrowInvalidOperationException_WhenIndexIsOutOfFreeDimensions()
    {
        Func<CubeResult<string, long>> action =
            () => Sut.Slice(2, null);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Slice_ShouldThrowInvalidOperationException_WhenDimensionNumbersAreDuplicating()
    {
        Func<CubeResult<string, long>> action =
            () => Sut.Slice((0, "A"), (0, "B"));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Slice_ReturnsSliceWithTotalValueValueFromSeed_WhenIndexValueIsMissing()
    {
        var seedData = 0;

        var slicedCube = Sut.Slice("No index");

        slicedCube.GetValue().Should().Be(seedData);
    }
}
