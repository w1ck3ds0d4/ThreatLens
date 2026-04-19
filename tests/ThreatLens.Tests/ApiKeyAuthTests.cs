using ThreatLens.Data;

namespace ThreatLens.Tests;

public class ApiKeyAuthTests
{
    [Fact]
    public void GeneratedKeysStartWithExpectedLabel()
    {
        var key = ApiKeyAuth.GenerateKey();
        Assert.StartsWith(ApiKeyAuth.KeyPrefixLabel, key);
        Assert.True(key.Length > ApiKeyAuth.KeyPrefixLabel.Length + 32);
    }

    [Fact]
    public void GeneratedKeysAreUnique()
    {
        var seen = new HashSet<string>();
        for (var i = 0; i < 100; i++)
        {
            Assert.True(seen.Add(ApiKeyAuth.GenerateKey()));
        }
    }

    [Fact]
    public void HashIsStableForSameInput()
    {
        var key = ApiKeyAuth.GenerateKey();
        Assert.Equal(ApiKeyAuth.HashKey(key), ApiKeyAuth.HashKey(key));
    }

    [Fact]
    public void HashDiffersForDifferentInputs()
    {
        var a = ApiKeyAuth.GenerateKey();
        var b = ApiKeyAuth.GenerateKey();
        Assert.NotEqual(ApiKeyAuth.HashKey(a), ApiKeyAuth.HashKey(b));
    }

    [Fact]
    public void HashIsHex64Chars()
    {
        var hash = ApiKeyAuth.HashKey("anything");
        Assert.Equal(64, hash.Length);
        Assert.All(hash, c => Assert.True(char.IsDigit(c) || (c >= 'a' && c <= 'f')));
    }

    [Fact]
    public void PrefixIsLabelPlusEightChars()
    {
        var key = ApiKeyAuth.GenerateKey();
        var prefix = ApiKeyAuth.PrefixFor(key);
        Assert.Equal(ApiKeyAuth.KeyPrefixLabel.Length + 8, prefix.Length);
        Assert.StartsWith(ApiKeyAuth.KeyPrefixLabel, prefix);
        Assert.Equal(key[..prefix.Length], prefix);
    }
}
