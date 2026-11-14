using TradeLedger.Api.Infrastructure.Security;
using Xunit;

namespace TradeLedger.Api.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_with_correct_password_returns_true()
    {
        var (hash, salt) = PasswordHasher.Hash("CorrectHorse#1");

        Assert.True(PasswordHasher.Verify("CorrectHorse#1", hash, salt));
    }

    [Fact]
    public void Verify_with_wrong_password_returns_false()
    {
        var (hash, salt) = PasswordHasher.Hash("CorrectHorse#1");

        Assert.False(PasswordHasher.Verify("WrongPassword#1", hash, salt));
    }

    [Fact]
    public void Hash_produces_a_different_salt_each_time()
    {
        var (_, saltA) = PasswordHasher.Hash("SamePassword#1");
        var (_, saltB) = PasswordHasher.Hash("SamePassword#1");

        Assert.NotEqual(saltA, saltB);
    }

    [Fact]
    public void Hash_with_same_password_and_different_salt_produces_different_hashes()
    {
        var (hashA, _) = PasswordHasher.Hash("SamePassword#1");
        var (hashB, _) = PasswordHasher.Hash("SamePassword#1");

        Assert.NotEqual(hashA, hashB);
    }
}
