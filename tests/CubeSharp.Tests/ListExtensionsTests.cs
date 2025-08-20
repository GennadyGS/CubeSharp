using CubeSharp.Tests.Utils;
using CubeSharp.Utils;
using FluentAssertions;
using Xunit;

namespace CubeSharp.Tests;

public sealed class ListExtensionsTests
{
    private static readonly int[] SourceArr0 = new[] { 3, 4 };
    private static readonly int[] SourceArr1 = new[] { 1, 2 };
    private static readonly int[] SourceArr2 = new[] { 3, 4 };
    private static readonly int[] SourceArr3 = new[] { 1, 3 };
    private static readonly int[] SourceArr4 = new[] { 1, 4 };
    private static readonly int[] SourceArr5 = new[] { 2, 3 };
    private static readonly int[] SourceArr6 = new[] { 2, 4 };
    private static readonly int[] SourceArr7 = new[] { 5, 6 };
    private static readonly int[] SourceArr8 = new[] { 1, 3, 5 };
    private static readonly int[] SourceArr9 = new[] { 1, 3, 6 };
    private static readonly int[] SourceArr10 = new[] { 1, 4, 5 };
    private static readonly int[] SourceArr11 = new[] { 1, 4, 6 };
    private static readonly int[] SourceArr12 = new[] { 2, 3, 5 };
    private static readonly int[] SourceArr13 = new[] { 2, 3, 6 };
    private static readonly int[] SourceArr14 = new[] { 2, 4, 5 };
    private static readonly int[] SourceArr15 = new[] { 2, 4, 6 };
    private static readonly int[] SourceArr16 = new[] { 1, 2, 3, 4 };
    private static readonly int[] SingleElementArr0 = new[] { 1 };
    private static readonly int[] SingleElementArr1 = new[] { 2 };
    private static readonly int[] SingleElementArr2 = new[] { 3 };
    private static readonly int[] SingleElementArr3 = new[] { 4 };

    public static TheoryData<IEnumerable<TestCaseModel>> InputSets =>
        TheoryDataBuilder.TheoryData(GetTestData());

    [Fact]
    public void SetValues_ShouldThrowException()
    {
        var list = new List<string> { "1", "2", "3" };
        var indexes = new List<int> { 1, 2 };
        var values = new List<string> { "1", "2", "3" };

        Action action = () => list.SetValues(indexes, values);

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(InputSets))]
    public void GetAllCombinations_ReturnsCorrectCartesianProduct_ForDifferentSets(
        IEnumerable<TestCaseModel> testCases)
    {
        foreach (var testCase in testCases)
        {
            var actual = testCase.Input.GetAllCombinations();
            actual.Should().BeEquivalentTo(testCase.ExpectedOutput);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Layout",
        "MEN003:Method is too long",
        Justification = "Data declaration, no logic")]
    private static IEnumerable<TestCaseModel> GetTestData() =>
        new List<TestCaseModel>
        {
            new TestCaseModel
            {
                Input = new[]
                {
                    Array.Empty<int>(),
                    SourceArr0,
                },
                ExpectedOutput = Array.Empty<int[]>(),
            },
            new TestCaseModel
            {
                Input = new[]
                {
                    Array.Empty<int>(),
                },
                ExpectedOutput = Array.Empty<int[]>(),
            },
            new TestCaseModel
            {
                Input = Array.Empty<int[]>(),
                ExpectedOutput = new[]
                {
                    Array.Empty<int>(),
                },
            },
            new TestCaseModel
            {
                Input = new[]
                {
                    SourceArr1,
                    SourceArr2,
                },
                ExpectedOutput = new[]
                {
                    SourceArr3,
                    SourceArr4,
                    SourceArr5,
                    SourceArr6,
                },
            },
            new TestCaseModel
            {
                Input = new[]
                {
                    SourceArr1,
                    SourceArr2,
                    SourceArr7,
                },
                ExpectedOutput = new[]
                {
                    SourceArr8,
                    SourceArr9,
                    SourceArr10,
                    SourceArr11,
                    SourceArr12,
                    SourceArr13,
                    SourceArr14,
                    SourceArr15,
                },
            },
            new TestCaseModel
            {
                Input = new[]
                {
                    SourceArr16,
                },
                ExpectedOutput = new List<IReadOnlyList<int>>
                {
                    SingleElementArr0,
                    SingleElementArr1,
                    SingleElementArr2,
                    SingleElementArr3,
                },
            },
            new TestCaseModel
            {
                Input = new[]
                {
                    SingleElementArr0,
                    SingleElementArr1,
                    SingleElementArr2,
                    SingleElementArr3,
                },
                ExpectedOutput = new[]
                {
                    SourceArr16,
                },
            },
        };

    public sealed record TestCaseModel
    {
        public required IReadOnlyList<IReadOnlyList<int>> Input { get; init; }

        public required IReadOnlyList<IReadOnlyList<int>> ExpectedOutput { get; init; }
    }
}
