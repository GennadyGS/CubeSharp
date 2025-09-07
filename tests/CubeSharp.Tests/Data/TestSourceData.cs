namespace CubeSharp.Tests.Data;

internal static class TestSourceData
{
    public static readonly TestSourceRecord[] Records =
    [
        new() { A = "1", B = 1, C = 11, D = 100, E = [1, 2] },
        new() { A = "3", B = 1, C = 11, D = null, E = [1] },
        new() { A = "1", B = 2, C = 22, D = 250, E = [2, 3] },
        new() { A = "1", B = 2, C = 11, D = 300, E = [] },
        new() { A = "1", B = 4, C = 22, D = 250, E = [4, 4, 6] },
        new() { A = "1", B = 0, C = 22, D = 180, E = [1, 3, 4] },
        new() { A = null, B = 2, C = 22, D = 320, E = [3, 2] },
        new() { A = "1", B = 3, C = 22, D = 50, E = [4, 2, 1] },
        new() { A = "4", B = 4, C = 22, D = 40, E = [0, 3] },
        new() { A = "2", B = 1, C = 11, D = 300, E = [2, 1] },
        new() { A = "1", B = 2, C = 22, D = 340, E = [5] },
        new() { A = "2", B = 2, C = 11, D = 100, E = [2] },
        new() { A = "2", B = 3, C = 22, D = 150, E = [3, 4] },
        new() { A = "2", B = 2, C = 44, D = 250, E = [4] },
        new() { A = "1", B = 0, C = 0, D = 290, E = [0] },
    ];
}
