using Xunit;

namespace Cube.Tests.Utils;

internal static class TheoryDataBuilder
{
    public static TheoryData<T> TheoryData<T>(params T[] testCases)
    {
        var result = new TheoryData<T>();

        foreach (var testCase in testCases)
        {
            result.Add(testCase);
        }

        return result;
    }
}
