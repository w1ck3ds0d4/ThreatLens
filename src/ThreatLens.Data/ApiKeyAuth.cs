using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ThreatLens.Domain;

namespace ThreatLens.Data;

/// Helpers for issuing and validating API keys. Keys are 32 random bytes
/// encoded as URL-safe base64 ("tl_" prefix for human recognition). Only the
/// SHA-256 hash is persisted; the plaintext key is shown once at creation.
public static class ApiKeyAuth
{
    public const string KeyPrefixLabel = "tl_";

    /// Minimum age of a LastUsedAt stamp before the middleware is willing to
    /// update it again. Keeps authenticated traffic from turning into a write
    /// storm on the api_keys table.
    public static readonly TimeSpan LastUsedWriteInterval = TimeSpan.FromMinutes(5);

    public static string GenerateKey()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return KeyPrefixLabel + Base64UrlEncode(bytes);
    }

    public static string HashKey(string rawKey)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(rawKey), hash);
        return Convert.ToHexStringLower(hash);
    }

    /// First 8 characters past the label, safe to log and show in UIs.
    public static string PrefixFor(string rawKey)
    {
        var len = Math.Min(rawKey.Length, KeyPrefixLabel.Length + 8);
        return rawKey[..len];
    }

    /// Look up and validate a presented key. Returns null on any failure
    /// (not found, revoked, hash mismatch) so callers can treat all auth
    /// failures identically. Updates LastUsedAt at most once per
    /// LastUsedWriteInterval to avoid write amplification.
    public static async Task<ApiKey?> ValidateAsync(
        ThreatLensDbContext db,
        string presentedKey,
        CancellationToken ct)
    {
        var hash = HashKey(presentedKey);
        var row = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == hash, ct);
        if (row is null) return null;
        if (row.RevokedAt is not null) return null;
        if (!FixedTimeEquals(row.KeyHash, hash)) return null;

        var now = DateTimeOffset.UtcNow;
        if (row.LastUsedAt is null || now - row.LastUsedAt.Value >= LastUsedWriteInterval)
        {
            row.LastUsedAt = now;
            await db.SaveChangesAsync(ct);
        }
        return row;
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(a),
            Encoding.ASCII.GetBytes(b));
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
