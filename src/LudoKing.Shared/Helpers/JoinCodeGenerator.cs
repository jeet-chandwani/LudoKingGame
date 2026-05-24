namespace LudoKing.Shared.Helpers;

public static class JoinCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly Random _rng = Random.Shared;

    public static string Generate()
    {
        var chars = new char[6];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = Alphabet[_rng.Next(Alphabet.Length)];
        return new string(chars);
    }
}
