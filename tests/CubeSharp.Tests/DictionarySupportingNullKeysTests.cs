using System.Collections;
using Cube.Utils;
using CubeSharp.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace CubeSharp.Tests;

public sealed class DictionarySupportingNullKeysTests
{
    private static readonly KeyValuePair<string?, int>[] InputWithNull =
    {
        KeyValuePair.Create((string?)"A", 1),
        KeyValuePair.Create((string?)"B", 2),
        KeyValuePair.Create((string?)null, 3),
    };

    private static readonly KeyValuePair<string, int>[] InputWithoutNull =
    {
        KeyValuePair.Create("A", 1),
        KeyValuePair.Create("B", 2),
    };

    public static TheoryData<KeyValuePair<string?, int>[]> TestInputs =>
        TheoryDataBuilder.TheoryData(InputWithNull, GetCastedInputWithoutNull());

    [Fact]
    public void Constructor_Should_ThrowArgumentException_WhenDefaultKeyIsDuplicated()
    {
        Action action = () =>
            new DictionarySupportingNullKeys<string, int>(
                new[]
                {
                    KeyValuePair.Create((string?)default, 1),
                    KeyValuePair.Create((string?)default, 2),
                });

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(TestInputs))]
    public void Count_ShouldReturnCorrectResult(KeyValuePair<string?, int>[] input)
    {
        var sut = new DictionarySupportingNullKeys<string, int>(input);

        var result = sut.Count;

        result.Should().Be(input.Length);
    }

    [Theory]
    [MemberData(nameof(TestInputs))]
    public void Keys_ShouldReturnCorrectResult(KeyValuePair<string?, int>[] input)
    {
        var sut = new DictionarySupportingNullKeys<string, int>(input);

        var result = sut.Keys;

        result.Should().BeEquivalentTo(input.Select(kvp => kvp.Key));
    }

    [Theory]
    [MemberData(nameof(TestInputs))]
    public void Values_ShouldReturnCorrectResult(KeyValuePair<string?, int>[] input)
    {
        var sut = new DictionarySupportingNullKeys<string, int>(input);

        var result = sut.Values;

        result.Should().BeEquivalentTo(input.Select(kvp => kvp.Value));
    }

    [Theory]
    [MemberData(nameof(TestInputs))]
    public void Index_ShouldReturnCorrectResult_WhenKeyIsNotNull(KeyValuePair<string?, int>[] input)
    {
        var sut = new DictionarySupportingNullKeys<string, int>(input);

        foreach (var key in GetAllKeys())
        {
            var result = sut[key];
            result.Should().Be(input.Single(kvp => kvp.Key == key).Value);
        }
    }

    [Fact]
    public void Index_ShouldReturnCorrectResult_WhenKeyIsNullAndContainsNullKey()
    {
        var sut = new DictionarySupportingNullKeys<string, int>(InputWithNull);

        var result = sut[null];
        result.Should().Be(InputWithNull.Single(kvp => kvp.Key == null).Value);
    }

    [Fact]
    public void Index_ShouldThrowKeyNotFountException_WhenKeyIsNullAndDoesNotContainsNullKey()
    {
        var sut = new DictionarySupportingNullKeys<string, int>(GetCastedInputWithoutNull());

        Func<int> action = () => sut[default];

        action.Should().Throw<KeyNotFoundException>();
    }

    [Theory]
    [MemberData(nameof(TestInputs))]
    public void GetEnumerator_ShouldReturnCorrectResult(KeyValuePair<string?, int>[] input)
    {
        var sut = new DictionarySupportingNullKeys<string, int>(input);

        var result = sut.AsEnumerable().ToList();

        result.Should().BeEquivalentTo(input);
    }

    [Fact]
    public void NonGenericEnumerator_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        var sut = new DictionarySupportingNullKeys<string, int>([]);

        var result = ((IEnumerable)sut).GetEnumerator();

        result.MoveNext().Should().BeFalse();
    }

    private static KeyValuePair<string?, int>[] GetCastedInputWithoutNull() =>
        InputWithoutNull
            .Select(kvp => KeyValuePair.Create((string?)kvp.Key, kvp.Value))
            .ToArray();

    private static IReadOnlyCollection<string> GetAllKeys() =>
        InputWithoutNull.Select(kvp => kvp.Key).ToList();
}
