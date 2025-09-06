using System.Collections;
using FluentAssertions;
using Xunit;

namespace CubeSharp.Tests;

public sealed class DimensionDefinitionTests
{
    private static readonly string[] SourceArray0 = new[] { "1", "2", "21", "22", "3" };
    private static readonly string[] SourceArra1 = new[] { "1", "21", "22", "2", "3" };
    private static readonly string[] LargeSourceArray =
        new[] { "1", "11", "12", "2", "21", "221", "222", "22", "3", "31" };

    [Fact]
    public void Enumerator_ShouldReturnEmpty_WhenDefinitionIsEmpty()
    {
        var sut = DimensionDefinition.Create<string, string>(s => s);

        var result = sut.AsEnumerable();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Enumerator_ShouldReturnIndexDefinitionsInCorrectOrder()
    {
        var sut = DimensionDefinition.Create(
            (string s) => s,
            null,
            IndexDefinition.Create("1"),
            IndexDefinition.Create("2"),
            IndexDefinition.Create("3"));

        var result = sut.AsEnumerable();

        result.Should().BeEquivalentTo(
            new[]
            {
                IndexDefinition.Create("1"),
                IndexDefinition.Create("2"),
                IndexDefinition.Create("3"),
            },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void Enumerator_ShouldReturnIndexDefinitionsWithParentOnTopInCorrectOrder()
    {
        var sut = DimensionDefinition.Create(
            (string s) => s,
            null,
            IndexDefinition.Create("1"),
            IndexDefinition.Create(
                "2",
                null,
                IndexDefinition.Create("21"),
                IndexDefinition.Create("22")),
            IndexDefinition.Create("3"));

        var result = sut.AsEnumerable().Select(def => def.Value).ToList();

        result.Should().BeEquivalentTo(
            SourceArray0,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void Enumerator_ShouldReturnIndexDefinitionsWithParentOnBottomInCorrectOrder()
    {
        var sut = DimensionDefinition.Create(
            (string s) => s,
            null,
            IndexDefinition.Create("1"),
            IndexDefinition.Create(
                new[]
                {
                    IndexDefinition.Create("21"),
                    IndexDefinition.Create("22"),
                },
                "2"),
            IndexDefinition.Create("3"));

        var result = sut.AsEnumerable().Select(def => def.Value).ToList();

        result.Should().BeEquivalentTo(
            SourceArra1,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void Enumerator_ShouldReturnIndexDefinitionsHierarchyInCorrectOrder()
    {
        var sut = DimensionDefinition.Create(
            (string s) => s,
            null,
            IndexDefinition.Create(
                "1",
                null,
                IndexDefinition.Create("11"),
                IndexDefinition.Create("12")),
            IndexDefinition.Create(
                "2",
                null,
                IndexDefinition.Create("21"),
                IndexDefinition.Create(
                    new[]
                    {
                        IndexDefinition.Create("221"),
                        IndexDefinition.Create("222"),
                    },
                    "22")),
            IndexDefinition.Create(
                "3",
                null,
                IndexDefinition.Create("31")));
        sut.AsEnumerable().Select(def => def.Value).Should().BeEquivalentTo(
            LargeSourceArray,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void NonGenericEnumerator_ShouldReturnEmpty_WhenDefinitionIsEmpty()
    {
        var sut = DimensionDefinition.Create<string, string>(s => s);

        var result = ((IEnumerable)sut).GetEnumerator();

        result.MoveNext().Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Some title")]
    public void Create_ShouldSetTitle(string title)
    {
        var sut = DimensionDefinition.Create((string s) => s, title);

        var result = sut.Title;

        result.Should().Be(title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Some title")]
    public void WithTitle_ShouldSetTitle(string title)
    {
        var sut = DimensionDefinition
            .Create((string s) => s)
            .WithTitle(title);

        var result = sut.Title;

        result.Should().Be(title);
    }

    [Fact]
    public void Create_ShouldLeaveMetadataEmpty()
    {
        var sut = DimensionDefinition.Create((string s) => s);

        var result = sut.Metadata;

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("A", "1")]
    [InlineData("A", null)]
    [InlineData("B", 3)]
    public void WithMetaData_ShouldSetMetaData(string key, object value)
    {
        var sut = DimensionDefinition
            .Create((string s) => s)
            .WithMetadata(key, value);

        var result = sut.Metadata;

        result.Single().Should().BeEquivalentTo(new KeyValuePair<string, object>(key, value));
    }

    [Theory]
    [InlineData("A", "1")]
    [InlineData("A", null)]
    [InlineData("B", 3)]
    public void WithMetaData_ShouldSetMetaData_WhenTuplePassed(string key, object value)
    {
        var sut = DimensionDefinition
            .Create((string s) => s)
            .WithMetadata((key, value));

        var result = sut.Metadata;

        result.Single().Should().BeEquivalentTo(new KeyValuePair<string, object>(key, value));
    }

    [Theory]
    [InlineData("A", "foo", "B", 2)]
    [InlineData("A", true, "B", null)]
    public void WithMetaData_ShouldSetMultipleMetaDataEntries(
        string key1, object value1, string key2, object value2)
    {
        var sut = DimensionDefinition
            .Create((string s) => s)
            .WithMetadata((key1, value1), (key2, value2));

        var result = sut.Metadata;

        result.Should().Equal(
            new KeyValuePair<string, object>(key1, value1),
            new KeyValuePair<string, object>(key2, value2));
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenIndexesHaveDuplicates()
    {
        Action action = () => DimensionDefinition.Create(
            (string s) => s,
            null,
            IndexDefinition.Create("1"),
            IndexDefinition.Create("2"),
            IndexDefinition.Create("1"));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenIndexesHaveDuplicatedDefaultIndexes()
    {
        Action action = () => DimensionDefinition.Create(
            (string s) => s,
            null,
            IndexDefinition.Create(default(string)),
            IndexDefinition.Create("a"),
            IndexDefinition.Create(default(string)));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenIndexesHaveNestedDuplicates()
    {
        Action action = () => DimensionDefinition.Create(
            (string s) => s,
            null,
            IndexDefinition.Create(
                "1",
                null,
                IndexDefinition.Create("3")),
            IndexDefinition.Create(
                "2",
                null,
                IndexDefinition.Create("3")));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateDefault_ShouldCreateDimensionWithCorrectIndexSelector()
    {
        var result = DimensionDefinition.CreateDefault<string, string>();

        result.IndexSelector.Compile()
            .Invoke("A")
            .Should().BeEquivalentTo(new object?[] { null });
    }

    [Fact]
    public void CreateDefault_ShouldCreateDimensionWithSingleDefaultIndex()
    {
        var result = DimensionDefinition.CreateDefault<string, string>();

        result.IndexDefinitions.Single()
            .Should().BeEquivalentTo(IndexDefinition.Create((string?)null));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData(null)]
    public void CreateDefault_ShouldCreateDimensionWithCorrectTitle(string title)
    {
        var result = DimensionDefinition.CreateDefault<string, string>(title);

        result.Title.Should().Be(title);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData(null)]
    public void CreateDefault_ShouldCreateDimensionWithCorrectIndexTitle(string indexTitle)
    {
        var result = DimensionDefinition.CreateDefault<string, string>(null, indexTitle);

        result[null].Title.Should().Be(indexTitle);
    }

    [Fact]
    public void CreateForDictionaryCollection_ShouldCreateDimensionDefinitionWithCorrectIndexSelector()
    {
        var result = DimensionDefinition.CreateForDictionaryCollection(
            r => (string?)r["A"] + (string?)r["B"]);

        result.IndexSelector.Compile()
            .Invoke(new Dictionary<string, object?> { ["A"] = "a", ["B"] = "b" })
            .Should().BeEquivalentTo("ab");
    }

    [Fact]
    public void CreateForDictionaryCollection_ShouldCreateDimensionDefinitionWithCorrectIndexes()
    {
        var result = DimensionDefinition.CreateForDictionaryCollection(
            r => (string?)r["A"],
            null,
            IndexDefinition.Create("1"),
            IndexDefinition.Create("2"));

        result.IndexDefinitions
            .Should().BeEquivalentTo(
                new[]
                {
                    IndexDefinition.Create("1"),
                    IndexDefinition.Create("2"),
                },
                options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData(null)]
    public void CreateForDictionaryCollection_ShouldCreateDimensionDefinitionWithCorrectTitle(string title)
    {
        var result = DimensionDefinition.CreateForDictionaryCollection(
            r => (string?)null,
            title);

        result.Title.Should().Be(title);
    }

    [Fact]
    public void CreateDefaultForDictionaryCollection_ShouldCreateDimensionWithCorrectIndexSelector()
    {
        var result = DimensionDefinition.CreateDefaultForDictionaryCollection<string>();

        result.IndexSelector.Compile()
            .Invoke(new Dictionary<string, object?>())
            .Should().BeEquivalentTo(new object?[] { null });
    }

    [Fact]
    public void CreateDefaultForDictionaryCollection_ShouldCreateDimensionWithSingleDefaultIndex()
    {
        var result = DimensionDefinition.CreateDefaultForDictionaryCollection<string>();

        result.IndexDefinitions.Single()
            .Should().BeEquivalentTo(IndexDefinition.Create((string?)null));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData(null)]
    public void CreateDefaultForDictionaryCollection_ShouldCreateDimensionWithCorrectTitle(string title)
    {
        var result = DimensionDefinition.CreateDefaultForDictionaryCollection<string>(title);

        result.Title.Should().Be(title);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData(null)]
    public void CreateDefaultForDictionaryCollection_ShouldCreateDimensionWithCorrectIndexTitle(string indexTitle)
    {
        var result = DimensionDefinition.CreateDefaultForDictionaryCollection<string>(null, indexTitle);

        result[null].Title.Should().Be(indexTitle);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("b")]
    public void CreateForCollection_ShouldCreateDimensionDefinitionWithCorrectIndexSelector(string item)
    {
        var result = DimensionDefinition.CreateForCollection(
            Array.Empty<string>(),
            s => s + "1");

        result.IndexSelector.Compile()
            .Invoke(item)
            .Should().BeEquivalentTo(item + "1");
    }

    [Fact]
    public void CreateForCollection_ShouldCreateDimensionDefinitionWithCorrectIndexes()
    {
        var result = DimensionDefinition.CreateForCollection(
            Array.Empty<string>(),
            s => s,
            null,
            IndexDefinition.Create("1"),
            IndexDefinition.Create("2"));

        result.IndexDefinitions
            .Should().BeEquivalentTo(
                new[]
                {
                    IndexDefinition.Create("1"),
                    IndexDefinition.Create("2"),
                },
                options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData(null)]
    public void CreateForCollection_ShouldCreateDimensionDefinitionWithCorrectTitle(string title)
    {
        var result = DimensionDefinition.CreateForCollection(
            Array.Empty<string>(),
            s => s,
            title);

        result.Title.Should().Be(title);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenDefaultIndexNotInRoot()
    {
        Action action = () => DimensionDefinition.Create(
            (string s) => s,
            "dimension title",
            IndexDefinition.Create(
                "1",
                null,
                IndexDefinition.Create("3")),
            IndexDefinition.Create(
                null,
                null,
                IndexDefinition.Create("4")));

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData(null)]
    public void WithTrailingDefaultIndex_ShouldAddTrailingDefaultIndex(string title)
    {
        var sut = DimensionDefinition
            .Create(
                (string s) => s,
                null,
                IndexDefinition.Create("1"),
                IndexDefinition.Create("2"));
        var result = sut.WithTrailingDefaultIndex(title);
        result.AsEnumerable()
            .Should().BeEquivalentTo(
                new[]
                {
                    IndexDefinition.Create("1"),
                    IndexDefinition.Create("2"),
                    IndexDefinition.Create(
                        new[]
                        {
                            IndexDefinition.Create("1"),
                            IndexDefinition.Create("2"),
                        },
                        null,
                        title),
                },
                options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData(null)]
    public void WithLeadingDefaultIndex_ShouldAddLeadingDefaultIndex(string title)
    {
        var sut = DimensionDefinition
            .Create(
                (string s) => s,
                null,
                IndexDefinition.Create("1"),
                IndexDefinition.Create("2"));
        var result = sut.WithLeadingDefaultIndex(title);
        result.AsEnumerable()
            .Should().BeEquivalentTo(
                new[]
                {
                    IndexDefinition.Create(
                        null,
                        title,
                        IndexDefinition.Create("1"),
                        IndexDefinition.Create("2")),
                    IndexDefinition.Create("1"),
                    IndexDefinition.Create("2"),
                },
                options => options.WithStrictOrdering());
    }

    [Fact]
    public void WithTrailingDefaultIndex_ShouldThrowArgumentException_WhenContainsDefaultIndex()
    {
        var sut = DimensionDefinition.CreateDefault<string, string>();
        Action act = () => sut.WithTrailingDefaultIndex();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithLeadingDefaultIndex_ShouldThrowArgumentException_WhenContainsDefaultIndex()
    {
        var sut = DimensionDefinition.CreateDefault<string, string>();
        Action act = () => sut.WithLeadingDefaultIndex();
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("21", "2")]
    [InlineData("22", "2")]
    [InlineData("1", null)]
    [InlineData("2", null)]
    [InlineData("3", null)]
    [InlineData(null, null)]
    public void GetParentIndex_ShouldReturnCorrectParentIndex(string index, string expectedResult)
    {
        var sut = DimensionDefinition
            .Create(
                (string s) => s,
                null,
                IndexDefinition.Create("1"),
                IndexDefinition.Create(
                    "2",
                    null,
                    IndexDefinition.Create("21"),
                    IndexDefinition.Create("22")));
        var result = sut.GetParentIndex(index);
        result.Should().Be(expectedResult);
    }
}
