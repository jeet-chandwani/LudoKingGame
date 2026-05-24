using System.Security.Cryptography;
using System.Text;

namespace LudoKing.Domain.Utilities;

public static class TokenHelper
{
    private const char Separator = '|';

    public static string GenerateToken(Guid userId, string purpose, string secret, TimeSpan expiry)
    {
        long expiryUnix = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
        string payload = $"{userId}{Separator}{purpose}{Separator}{expiryUnix}";
        string hmac = ComputeHmac(payload, secret);
        string full = $"{payload}{Separator}{hmac}";
        return Base64UrlEncode(Encoding.UTF8.GetBytes(full));
    }

    public static Guid? ValidateToken(string token, string purpose, string secret)
    {
        try
        {
            string decoded = Encoding.UTF8.GetString(Base64UrlDecode(token));
            // format: userId|purpose|expiryUnix|hmac
            int lastSep = decoded.LastIndexOf(Separator);
            if (lastSep < 0)
                return null;

            string payload = decoded[..lastSep];
            string providedHmac = decoded[(lastSep + 1)..];
            string expectedHmac = ComputeHmac(payload, secret);

            if (!CryptographicEquals(providedHmac, expectedHmac))
                return null;

            var parts = payload.Split(Separator);
            if (parts.Length != 3)
                return null;

            if (!Guid.TryParse(parts[0], out Guid userId))
                return null;

            if (!string.Equals(parts[1], purpose, StringComparison.Ordinal))
                return null;

            if (!long.TryParse(parts[2], out long expiryUnix))
                return null;

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiryUnix)
                return null;

            return userId;
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeHmac(string payload, string secret)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        byte[] hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToBase64String(hash);
    }

    private static bool CryptographicEquals(string a, string b)
    {
        byte[] aBytes = Encoding.UTF8.GetBytes(a);
        byte[] bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static string Base64UrlEncode(byte[] input)
        => Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        string padded = input.Replace('-', '+').Replace('_', '/');
        int pad = padded.Length % 4;
        if (pad == 2) padded += "==";
        else if (pad == 3) padded += "=";
        return Convert.FromBase64String(padded);
    }
}
