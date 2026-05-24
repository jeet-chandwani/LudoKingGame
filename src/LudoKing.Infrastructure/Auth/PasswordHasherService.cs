using System.Security.Cryptography;
using LudoKing.Interfaces.Infrastructure;

namespace LudoKing.Infrastructure.Auth;

public sealed class PasswordHasherService : IPasswordHasher
{
    private const int Iterations = 350_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA512, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool VerifyPassword(string hash, string password)
    {
        var parts = hash.Split('.');
        if (parts.Length != 3) return false;
        int iterations = int.Parse(parts[0]);
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] stored = Convert.FromBase64String(parts[2]);
        byte[] derived = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA512, stored.Length);
        return CryptographicOperations.FixedTimeEquals(derived, stored);
    }
}
