using CubeSharp.Tests.Data;
using FluentAssertions;
using Xunit;

namespace CubeSharp.Tests.CubeResultTests;

public sealed class GetValueTests
{
    public GetValueTests()
    {
        Sut = TestSourceData.Records.BuildCube(
            TestAggregationDefinitions.SumOfD,
            TestDimensionDefinitions.A,
            TestDimensionDefinitions.B);
    }

    private CubeResult<string, long> Sut { get; }

    [Fact]
    public void GetValue_ShouldReturnGrandTotal_WhenNoIndexesSpecified()
    {
        var result = Sut.GetValue();

        result.Should().Be(TestSourceData.Records.Sum(r => r.D));
    }

    [Fact]
    public void GetValue_ShouldReturnGrandTotal_WhenAllIndexesAreDefault()
    {
        var result = Sut.GetValue(null, null);

        result.Should().Be(TestSourceData.Records.Sum(r => r.D));
    }

    [Fact]
    public void GetValue_ShouldReturnGrandTotal_WhenFirstIndexIsDefaultAndSecondIndexIsNotSpecified()
    {
        var result = Sut.GetValue(default(string));

        result.Should().Be(TestSourceData.Records.Sum(r => r.D));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    public void GetValue_ShouldReturnTotalByFirstDimension_WhenSecondIndexIsDefault(
        string index)
    {
        var result = Sut.GetValue(index, null);

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    public void GetValue_ShouldReturnTotalByFirstDimension_WhenFirstIndexIsOmitted(
        string index)
    {
        var result = Sut.GetValue(index);

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    public void GetValue_ShouldReturnTotalBySecondDimension_WhenFirstIndexIsDefault(
        string index)
    {
        var result = Sut.GetValue(null, index);

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
    public void GetValue_ShouldReturnAggregatedValue_WhenAllIndexesAreInDefinitions(
        string index1, string index2)
    {
        var result = Sut.GetValue(index1, index2);

        result.Should().Be(
            TestSourceData.Records
                .Where(r => r.A == index1 && r.B.ToString() == index2)
                .Sum(r => r.D));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    public void GetValue_ShouldReturnSeedValue_WhenSingleIndexIsNotInDefinition(
        string index)
    {
        var result = Sut.GetValue(index);

        result.Should().Be(TestAggregationDefinitions.SumOfD.SeedValue);
    }

    [Theory]
    [InlineData("1", "0")]
    [InlineData("3", "1")]
    [InlineData(null, "0")]
    [InlineData("3", null)]
    [InlineData("4", "4")]
    public void GetValue_ShouldReturnSeedValue_WhenSomeIndexIsNotInDefinition(
        string index1, string index2)
    {
        var result = Sut.GetValue(index1, index2);

        result.Should().Be(TestAggregationDefinitions.SumOfD.SeedValue);
    }

    [Theory]
    [InlineData("1", "1", "1")]
    [InlineData(null, "1", "1")]
    [InlineData("1", "1", null)]
    [InlineData(null, null, null)]
    public void GetValue_ShouldThrowArgumentException_WhenTooManyIndexesIsSpecified(
        string index1, string index2, string index3)
    {
        Action action = () => Sut.GetValue(index1, index2, index3);

        action.Should().Throw<ArgumentException>();
    }
}
