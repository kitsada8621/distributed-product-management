using System.Text.RegularExpressions;
using Auth.Infrastructure.Security;

namespace Auth.IntegrationTests.Security;

public partial class SecureTokenServiceTests
{
    private const string Secret = "test-secret-test-secret-test-secret-01";

    private readonly SecureTokenService _service = new(new CsrfOptions { Secret = Secret });
    private readonly Guid _familyId = Guid.NewGuid();

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex UrlSafe();

    [Fact]
    public void GenerateOpaqueToken_IsUniqueAndUrlSafe()
    {
        var first = _service.GenerateOpaqueToken();
        var second = _service.GenerateOpaqueToken();

        Assert.NotEqual(first, second);
        Assert.Matches(UrlSafe(), first);
        Assert.Equal(43, first.Length);
    }

    [Fact]
    public void Hash_IsStableLowercaseHexAndDiffersFromInput()
    {
        var hash = _service.Hash("raw-token");

        Assert.Equal(hash, _service.Hash("raw-token"));
        Assert.NotEqual("raw-token", hash);
        Assert.Matches("^[0-9a-f]{64}$", hash);
        Assert.NotEqual(hash, _service.Hash("raw-token2"));
    }

    [Fact]
    public void CsrfToken_ValidatesForItsOwnFamily()
    {
        var csrf = _service.CreateCsrfToken(_familyId);

        Assert.True(_service.IsValidCsrfToken(csrf, _familyId));
    }

    [Fact]
    public void CsrfToken_FailsForAnotherFamily()
    {
        var csrf = _service.CreateCsrfToken(_familyId);

        Assert.False(_service.IsValidCsrfToken(csrf, Guid.NewGuid()));
    }

    [Fact]
    public void CsrfToken_FailsWhenSignatureIsTampered()
    {
        var csrf = _service.CreateCsrfToken(_familyId);
        var parts = csrf.Split('.');
        var tamperedSignature = (parts[1][0] == 'A' ? 'B' : 'A') + parts[1][1..];

        Assert.False(_service.IsValidCsrfToken($"{parts[0]}.{tamperedSignature}", _familyId));
    }

    [Fact]
    public void CsrfToken_FailsWhenNonceIsTampered()
    {
        var csrf = _service.CreateCsrfToken(_familyId);
        var parts = csrf.Split('.');
        var tamperedNonce = (parts[0][0] == 'A' ? 'B' : 'A') + parts[0][1..];

        Assert.False(_service.IsValidCsrfToken($"{tamperedNonce}.{parts[1]}", _familyId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-dot-here")]
    [InlineData(".")]
    [InlineData("a.b.c")]
    public void CsrfToken_MalformedInput_ReturnsFalse(string malformed)
    {
        Assert.False(_service.IsValidCsrfToken(malformed, _familyId));
    }

    [Fact]
    public void CsrfToken_TwoTokensForTheSameFamily_Differ()
    {
        Assert.NotEqual(_service.CreateCsrfToken(_familyId), _service.CreateCsrfToken(_familyId));
    }

    [Fact]
    public void CsrfToken_SignedWithAnotherSecret_FailsValidation()
    {
        var other = new SecureTokenService(new CsrfOptions { Secret = "another-secret-another-secret-0123456" });
        var csrf = other.CreateCsrfToken(_familyId);

        Assert.False(_service.IsValidCsrfToken(csrf, _familyId));
    }

    [Fact]
    public void Constructor_ShortSecret_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new SecureTokenService(new CsrfOptions { Secret = "too-short" }));
    }
}
