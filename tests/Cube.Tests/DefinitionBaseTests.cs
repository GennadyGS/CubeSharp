using FluentAssertions;
using Xunit;

namespace Cube.Tests;

public sealed class DefinitionBaseTests
{
    [Theory]
    [InlineData("key1", "1")]
    [InlineData("key2", 2)]
    [InlineData("key3", null)]
    public void AddMetadata_ShouldSetMetadataCorrectly(string key, object value)
    {
        var sut = new DefinitionBase("Title");

        sut.AddMetadata(key, value);

        var metadata = sut.Metadata;
        metadata[key].Should().Be(value);
    }

    [Fact]
    public void AddMetadata_ThrowsException_WhenKeyIsNull()
    {
        var sut = new DefinitionBase("Title");
        Action action = () => sut.AddMetadata(null, 1);
        action.Should().Throw<ArgumentNullException>();
    }
}
