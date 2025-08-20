using FluentAssertions;
using Xunit;

namespace CubeSharp.Tests;

public sealed class AggregationDefinitionTests
{
    [Theory]
    [InlineData(2, 4)]
    [InlineData(-2, 4)]
    [InlineData(0, 0)]
    public void ValueSelector_ReturnsCorrectValue_WhenCreatedByCreateMethod(int input, int expected)
    {
        var aggregation = AggregationDefinition.Create(
            (int i) => i * i,
            (a, b) => a + b,
            0);

        var actualValue = aggregation.ValueSelector.Compile().Invoke(input);

        actualValue.Should().Be(expected);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(-2, 4)]
    [InlineData(0, 0)]
    public void SelectValue_ReturnsCorrectValue_WhenCreatedByCreateMethod(int input, int expected)
    {
        var aggregation = AggregationDefinition.Create(
            (int i) => i * i,
            (a, b) => a + b,
            0);

        var actualValue = aggregation.SelectValue(input);

        actualValue.Should().Be(expected);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(-2, -4)]
    [InlineData(0, 0)]
    public void SelectValue_ReturnsCorrectValue_WhenCreatedByCreateForCollectionMethod(int input, int expected)
    {
        var numbers = Enumerable.Range(0, 10);
        var aggregation = AggregationDefinition.CreateForCollection(
            numbers,
            i => i * 2,
            (a, b) => a + b,
            0);

        var actualValue = aggregation.SelectValue(input);

        actualValue.Should().Be(expected);
    }

    [Theory]
    [InlineData("a", -1)]
    [InlineData("b", 0)]
    [InlineData("c", 1)]
    public void SelectValue_ReturnsCorrectValue_WhenCreatedByCreateForDictionaryMethod(string input, int expected)
    {
        var dict = new Dictionary<string, object>
        {
            ["a"] = -1,
            ["b"] = 0,
            ["c"] = 1,
        };

        var aggregation = AggregationDefinition.CreateForDictionaryCollection(
            d => (int)d[input],
            (a, b) => a + b,
            0);

        var actualValue = aggregation.SelectValue(dict);

        actualValue.Should().Be(expected);
    }
}
