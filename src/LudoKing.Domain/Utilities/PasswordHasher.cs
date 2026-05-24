using System.Security.Cryptography;
using LudoKing.Interfaces.Infrastructure;

namespace LudoKing.Domain.Utilities;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 350_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool VerifyPassword(string hash, string password)
    {
        var parts = hash.Split('.');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out int iterations))
            return false;

        byte[] salt;
        byte[] storedKey;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            storedKey = Convert.FromBase64String(parts[2]);
        }
        catch
        {
            return false;
        }

        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, storedKey.Length);
        return CryptographicOperations.FixedTimeEquals(derivedKey, storedKey);
    }
}
