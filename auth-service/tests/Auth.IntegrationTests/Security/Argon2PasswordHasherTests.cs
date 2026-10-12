using Auth.Infrastructure.Security;

namespace Auth.IntegrationTests.Security;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_IsArgon2idAndDoesNotContainThePassword()
    {
        var hash = _hasher.Hash("Passw0rd!");

        Assert.StartsWith("argon2id$", hash);
        Assert.DoesNotContain("Passw0rd!", hash);
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentStrings()
    {
        Assert.NotEqual(_hasher.Hash("Passw0rd!"), _hasher.Hash("Passw0rd!"));
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Passw0rd!");

        Assert.True(_hasher.Verify("Passw0rd!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Passw0rd!");

        Assert.False(_hasher.Verify("passw0rd!", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("argon2id$m=19456,t=2,p=1$not-base64$not-base64")]
    [InlineData("argon2id$m=x,t=2,p=1$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("bcrypt$m=19456,t=2,p=1$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    public void Verify_MalformedHash_ReturnsFalse(string malformed)
    {
        Assert.False(_hasher.Verify("Passw0rd!", malformed));
    }
}
