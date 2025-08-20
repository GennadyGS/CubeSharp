namespace CubeSharp.Tests.Data;

internal static class TestSourceData
{
    public static readonly TestSourceRecord[] Records =
    {
        new TestSourceRecord { A = "1", B = 1, C = 11, D = 100, E = new[] { 1, 2 } },
        new TestSourceRecord { A = "3", B = 1, C = 11, D = null, E = new[] { 1 } },
        new TestSourceRecord { A = "1", B = 2, C = 22, D = 250, E = new[] { 2, 3 } },
        new TestSourceRecord { A = "1", B = 2, C = 11, D = 300, E = Array.Empty<int>() },
        new TestSourceRecord { A = "1", B = 4, C = 22, D = 250, E = new[] { 4, 4, 6 } },
        new TestSourceRecord { A = "1", B = 0, C = 22, D = 180, E = new[] { 1, 3, 4 } },
        new TestSourceRecord { A = null, B = 2, C = 22, D = 320, E = new[] { 3, 2 } },
        new TestSourceRecord { A = "1", B = 3, C = 22, D = 50, E = new[] { 4, 2, 1 } },
        new TestSourceRecord { A = "4", B = 4, C = 22, D = 40, E = new[] { 0, 3 } },
        new TestSourceRecord { A = "2", B = 1, C = 11, D = 300, E = new[] { 2, 1 } },
        new TestSourceRecord { A = "1", B = 2, C = 22, D = 340, E = new[] { 5 } },
        new TestSourceRecord { A = "2", B = 2, C = 11, D = 100, E = new[] { 2 } },
        new TestSourceRecord { A = "2", B = 3, C = 22, D = 150, E = new[] { 3, 4 } },
        new TestSourceRecord { A = "2", B = 2, C = 44, D = 250, E = new[] { 4 } },
        new TestSourceRecord { A = "1", B = 0, C = 0, D = 290, E = new[] { 0 } },
    };
}
