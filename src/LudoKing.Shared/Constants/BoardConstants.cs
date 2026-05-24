namespace LudoKing.Shared.Constants;

public static class BoardConstants
{
    public const int OuterTrackLength = 52;
    public const int HomeStretchLength = 5;
    public const int HomeCenterSquare = 999;
    public const int YardSquare = -1;

    public static readonly string[] Colors = { "Red", "Blue", "Green", "Yellow" };

    public static readonly int[] SafeSquares = { 0, 8, 13, 21, 26, 34, 39, 47 };

    public static readonly Dictionary<string, int> ColorStartSquares = new()
    {
        { "Red",    0  },
        { "Blue",   13 },
        { "Green",  26 },
        { "Yellow", 39 }
    };

    public static readonly Dictionary<string, int> ColorHomeEntrySquares = new()
    {
        { "Red",    50 },
        { "Blue",   11 },
        { "Green",  24 },
        { "Yellow", 37 }
    };

    public static readonly Dictionary<string, int> HomeStretchStart = new()
    {
        { "Red",    100 },
        { "Blue",   110 },
        { "Green",  120 },
        { "Yellow", 130 }
    };

    public static bool IsSafeSquare(int square)
    {
        if (IsHomeStretchSquare(square)) return true;
        foreach (var s in SafeSquares)
            if (s == square) return true;
        return false;
    }

    public static bool IsHomeStretchSquare(int square) =>
        (square >= 100 && square <= 104) ||
        (square >= 110 && square <= 114) ||
        (square >= 120 && square <= 124) ||
        (square >= 130 && square <= 134);

    public static int GetHomeStretchStart(string color) => HomeStretchStart[color];

    public static int GetStartSquare(string color) => ColorStartSquares[color];

    public static int GetHomeEntrySquare(string color) => ColorHomeEntrySquares[color];
}
