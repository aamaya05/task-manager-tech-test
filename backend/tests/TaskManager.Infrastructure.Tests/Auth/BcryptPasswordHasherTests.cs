using FluentAssertions;
using TaskManager.Infrastructure.Auth;

namespace TaskManager.Infrastructure.Tests.Auth;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void BcryptPasswordHasher_Hash_ReturnsNonEmptyBcryptHash()
    {
        var hash = _hasher.Hash("MyP@ssw0rd!");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2");
    }

    [Fact]
    public void BcryptPasswordHasher_Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("MyP@ssw0rd!");

        var result = _hasher.Verify("MyP@ssw0rd!", hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void BcryptPasswordHasher_Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("CorrectPassword");

        var result = _hasher.Verify("WrongPassword", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void BcryptPasswordHasher_Hash_SamePlaintext_ProducesDifferentHashes()
    {
        var plaintext = "SamePassword123";

        var hash1 = _hasher.Hash(plaintext);
        var hash2 = _hasher.Hash(plaintext);

        hash1.Should().NotBe(hash2);
    }
}
