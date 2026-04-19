using ThreatLens.Data;

namespace ThreatLens.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void VerifyReturnsTrueForOriginalPassword()
    {
        const string pw = "correct horse battery staple";
        var hash = PasswordHasher.Hash(pw);
        Assert.True(PasswordHasher.Verify(pw, hash));
    }

    [Fact]
    public void VerifyReturnsFalseForWrongPassword()
    {
        var hash = PasswordHasher.Hash("one");
        Assert.False(PasswordHasher.Verify("two", hash));
    }

    [Fact]
    public void HashIncludesSchemeAndIterations()
    {
        var hash = PasswordHasher.Hash("anything");
        var parts = hash.Split('$');
        Assert.Equal(4, parts.Length);
        Assert.Equal("pbkdf2", parts[0]);
        Assert.True(int.Parse(parts[1]) >= 100_000);
    }

    [Fact]
    public void HashesAreSaltedSoNeverEqualForSameInput()
    {
        var a = PasswordHasher.Hash("same");
        var b = PasswordHasher.Hash("same");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MalformedHashDoesNotThrow()
    {
        Assert.False(PasswordHasher.Verify("x", "garbage"));
        Assert.False(PasswordHasher.Verify("x", "pbkdf2$100$zz$zz"));
        Assert.False(PasswordHasher.Verify("x", ""));
    }

    [Fact]
    public void GeneratedBootstrapPasswordsAreUrlSafeAndUnique()
    {
        var a = PasswordHasher.GenerateBootstrapPassword();
        var b = PasswordHasher.GenerateBootstrapPassword();
        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 20);
        Assert.DoesNotContain('+', a);
        Assert.DoesNotContain('/', a);
        Assert.DoesNotContain('=', a);
    }
}
