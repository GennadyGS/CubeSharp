using FluentAssertions;
using Xunit;

namespace CubeSharp.Tests;

public sealed class IndexDefinitionTests
{
    [Fact]
    public void Create_ShouldBeSuccess_WhenIndexIsDefault()
    {
        var result = IndexDefinition.Create((string?)null);

        result.Value.Should().Be(null);
    }

    [Fact]
    public void Create_ShouldBeSuccess_WhenDefaultIndexHasChildren()
    {
        var result = IndexDefinition.Create(
            null,
            null,
            IndexDefinition.Create("a"),
            IndexDefinition.Create("b"));

        result.Value.Should().Be(null);
        result.Children.Count.Should().Be(2);
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenDefaultIndexIsNested()
    {
        Action action = () => IndexDefinition.Create(
            "a",
            null,
            IndexDefinition.Create("b"),
            IndexDefinition.Create((string?)null));

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Some title")]
    public void Create_ShouldSetTitle(string title)
    {
        var sut = IndexDefinition.Create("1", title);

        var result = sut.Title;

        result.Should().Be(title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Some title")]
    public void WithTitle_ShouldSetTitle(string title)
    {
        var sut = IndexDefinition
            .Create("1")
            .WithTitle(title);

        var result = sut.Title;

        result.Should().Be(title);
    }

    [Fact]
    public void Create_ShouldLeaveMetadataEmpty()
    {
        var sut = IndexDefinition.Create("1");

        var result = sut.Metadata;

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("A", "1")]
    [InlineData("A", null)]
    [InlineData("B", 3)]
    public void WithMetaData_ShouldSetMetaData(string key, object value)
    {
        var sut = IndexDefinition
            .Create("1")
            .WithMetadata(key, value);

        var result = sut.Metadata;

        result.Single().Should().BeEquivalentTo(new KeyValuePair<string, object>(key, value));
    }

    [Theory]
    [InlineData("A", "foo", "B", 2)]
    [InlineData("A", true, "B", null)]
    public void WithMetaData_ShouldSetMultipleMetaDataEntries(
        string key1, object value1, string key2, object value2)
    {
        var sut = IndexDefinition
            .Create("1")
            .WithMetadata((key1, value1), (key2, value2));

        var result = sut.Metadata;

        result.Should().Equal(
            new KeyValuePair<string, object>(key1, value1),
            new KeyValuePair<string, object>(key2, value2));
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenChildrenHaveDuplicates()
    {
        Action action = () => IndexDefinition.Create(
            "1",
            null,
            IndexDefinition.Create("2"),
            IndexDefinition.Create("3"),
            IndexDefinition.Create("2"));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenChildrenHaveNestedDuplicates()
    {
        Action action = () => IndexDefinition.Create(
            "1",
            null,
            IndexDefinition.Create(
                "21",
                null,
                IndexDefinition.Create("3")),
            IndexDefinition.Create(
                "22",
                null,
                IndexDefinition.Create("3")));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenChildrenHaveNestedDuplicatesWithRoot()
    {
        Action action = () => IndexDefinition.Create(
            "1",
            null,
            IndexDefinition.Create(
                "2",
                null,
                IndexDefinition.Create("1")));

        action.Should().Throw<ArgumentException>();
    }
}
