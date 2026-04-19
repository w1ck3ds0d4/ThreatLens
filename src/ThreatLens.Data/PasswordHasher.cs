using System.Security.Cryptography;
using System.Text;

namespace ThreatLens.Data;

/// Minimal PBKDF2-SHA256 password hasher. Format: `pbkdf2$iterations$saltHex$hashHex`.
/// Reads the iteration count out of the stored hash so future bumps don't
/// invalidate existing passwords.
public static class PasswordHasher
{
    private const int Iterations = 200_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const string Scheme = "pbkdf2";

    public static string Hash(string password)
    {
        Span<byte> salt = stackalloc byte[SaltBytes];
        RandomNumberGenerator.Fill(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"{Scheme}${Iterations}${Convert.ToHexStringLower(salt)}${Convert.ToHexStringLower(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4) return false;
        if (!parts[0].Equals(Scheme, StringComparison.Ordinal)) return false;
        if (!int.TryParse(parts[1], out var iterations) || iterations < 1) return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromHexString(parts[2]);
            expected = Convert.FromHexString(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var derived = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(expected, derived);
    }

    /// Generate a random password suitable for a bootstrap admin. 18 bytes
    /// of entropy encoded as base64url yields ~24 URL-safe characters.
    public static string GenerateBootstrapPassword()
    {
        Span<byte> bytes = stackalloc byte[18];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
