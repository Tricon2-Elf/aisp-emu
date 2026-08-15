using aisp.Common;

namespace aisp.Common.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_Then_Verify_Succeeds()
    {
        var hash = PasswordHasher.Hash("correct horse battery staple");
        Assert.True(PasswordHasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_Fails_WhenPasswordWrong()
    {
        var hash = PasswordHasher.Hash("secret");
        Assert.False(PasswordHasher.Verify("wrong", hash));
    }

    [Fact]
    public void Verify_Fails_WhenStoredStringMalformed()
    {
        Assert.False(PasswordHasher.Verify("x", "not-a-valid-format"));
    }
}
