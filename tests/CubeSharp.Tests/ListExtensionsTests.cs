using CubeSharp.Tests.Utils;
using CubeSharp.Utils;
using FluentAssertions;
using Xunit;

namespace CubeSharp.Tests;

public sealed class ListExtensionsTests
{
    private static readonly int[] SourceArr0 = [3, 4];
    private static readonly int[] SourceArr1 = [1, 2];
    private static readonly int[] SourceArr2 = [3, 4];
    private static readonly int[] SourceArr3 = [1, 3];
    private static readonly int[] SourceArr4 = [1, 4];
    private static readonly int[] SourceArr5 = [2, 3];
    private static readonly int[] SourceArr6 = [2, 4];
    private static readonly int[] SourceArr7 = [5, 6];
    private static readonly int[] SourceArr8 = [1, 3, 5];
    private static readonly int[] SourceArr9 = [1, 3, 6];
    private static readonly int[] SourceArr10 = [1, 4, 5];
    private static readonly int[] SourceArr11 = [1, 4, 6];
    private static readonly int[] SourceArr12 = [2, 3, 5];
    private static readonly int[] SourceArr13 = [2, 3, 6];
    private static readonly int[] SourceArr14 = [2, 4, 5];
    private static readonly int[] SourceArr15 = [2, 4, 6];
    private static readonly int[] SourceArr16 = [1, 2, 3, 4];
    private static readonly int[] SingleElementArr0 = [1];
    private static readonly int[] SingleElementArr1 = [2];
    private static readonly int[] SingleElementArr2 = [3];
    private static readonly int[] SingleElementArr3 = [4];

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
                Input =
                [
                    [],
                    SourceArr0,
                ],
                ExpectedOutput = [],
            },
            new TestCaseModel
            {
                Input =
                [
                    [],
                ],
                ExpectedOutput = [],
            },
            new TestCaseModel
            {
                Input = [],
                ExpectedOutput =
                [
                    [],
                ],
            },
            new TestCaseModel
            {
                Input =
                [
                    SourceArr1,
                    SourceArr2,
                ],
                ExpectedOutput =
                [
                    SourceArr3,
                    SourceArr4,
                    SourceArr5,
                    SourceArr6,
                ],
            },
            new TestCaseModel
            {
                Input =
                [
                    SourceArr1,
                    SourceArr2,
                    SourceArr7,
                ],
                ExpectedOutput =
                [
                    SourceArr8,
                    SourceArr9,
                    SourceArr10,
                    SourceArr11,
                    SourceArr12,
                    SourceArr13,
                    SourceArr14,
                    SourceArr15,
                ],
            },
            new TestCaseModel
            {
                Input =
                [
                    SourceArr16,
                ],
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
                Input =
                [
                    SingleElementArr0,
                    SingleElementArr1,
                    SingleElementArr2,
                    SingleElementArr3,
                ],
                ExpectedOutput =
                [
                    SourceArr16,
                ],
            },
        };

    public sealed record TestCaseModel
    {
        public required IReadOnlyList<IReadOnlyList<int>> Input { get; init; }

        public required IReadOnlyList<IReadOnlyList<int>> ExpectedOutput { get; init; }
    }
}
