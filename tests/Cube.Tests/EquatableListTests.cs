using Cube.Utils;
using FluentAssertions;
using Xunit;

namespace Cube.Tests;

public sealed class EquatableListTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Count_ReturnsCorrectAmountValueOfElements(int size)
    {
        var list = GetList(0, size);

        list.Count.Should().Be(size);
    }

    [Theory]
    [InlineData(10, 4, 4)]
    [InlineData(3, 2, 2)]
    public void Indexer_ReturnsCorrectValueOfElementByIndex(int size, int index, int expected)
    {
        var list = GetList(0, size);

        list[index].Should().Be(expected);
    }

    [Fact]
    public void Indexer_ShouldThrowException_WhenIndexOutOfRange()
    {
        var list = GetList(0, 10);

        Func<int> func = () => list[100];

        func.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1, 2, false)]
    [InlineData(2, 2, true)]
    [InlineData(null, 2, false)]
    [InlineData(2, null, false)]
    [InlineData(null, null, true)]
    public void EqualOperator_ReturnsCorrectValue(int? leftSize, int? rightSize, bool expected)
    {
        var leftList = GetListOrNull(leftSize);
        var rightList = GetListOrNull(rightSize);

        var res = leftList! == rightList!;

        res.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 2, true)]
    [InlineData(2, 2, false)]
    [InlineData(null, 2, true)]
    [InlineData(2, null, true)]
    [InlineData(null, null, false)]
    public void NotEqualOperator_ReturnsCorrectValue(int? leftSize, int? rightSize, bool expected)
    {
        var leftList = GetListOrNull(leftSize);
        var rightList = GetListOrNull(rightSize);

        var res = leftList! != rightList!;

        res.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 2, false)]
    [InlineData(2, 2, true)]
    [InlineData(2, null, false)]
    public void Equals_ReturnsCorrectValue(int? leftSize, int? rightSize, bool expected)
    {
        var leftList = GetListOrNull(leftSize);
        var rightList = GetListOrNull(rightSize);

        var res = leftList!.Equals(rightList);

        leftList.Equals(leftList).Should().BeTrue();
        res.Should().Be(expected);
    }

    [Fact]
    public void Equals_ReturnsCorrectValue_WhenDifferentTypes()
    {
        var leftList = GetListOrNull(10);
        var rightList = Enumerable.Range(0, 10);

        var res = leftList!.Equals(rightList);

        res.Should().BeFalse();
    }

    [Fact]
    public void Equals_ReturnsCorrectValue_WhenNullAsObjectPassed()
    {
        var leftList = GetListOrNull(10);
        object? rightList = null;

        var res = leftList!.Equals(rightList);

        res.Should().BeFalse();
    }

    [Fact]
    public void Enumerator_ShouldBeEmpty()
    {
        var list = new EquatableList<int>(Enumerable.Empty<int>().ToList());

        list.AsEnumerable().Should().BeEmpty();
    }

    [Fact]
    public void Enumerator_ShouldReturnCorrectSequence()
    {
        var items = GetList(0, 10);
        var list = new EquatableList<int>(items);

        list.AsEnumerable().Should().BeEquivalentTo(items);
    }

    private EquatableList<int>? GetListOrNull(int? count) =>
        count.HasValue
            ? GetList(0, count.Value)
            : null;

    private EquatableList<int> GetList(int start, int count) =>
        new EquatableList<int>(Enumerable.Range(start, count).ToList());
}
