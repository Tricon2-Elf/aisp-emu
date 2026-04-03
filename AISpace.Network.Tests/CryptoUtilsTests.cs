using AISpace.Network.Crypto;

namespace AISpace.Network.Tests;

public class CryptoUtilsTests
{
    [Fact]
    public void GenerateOTP_HasLength20_AndHexLower()
    {
        var otp = CryptoUtils.GenerateOTP();
        Assert.Equal(20, otp.Length);
        Assert.Matches("^[0-9a-f]{20}$", otp);
    }

    [Fact]
    public void CreateEncryptedKey_ReturnsSixteenByteCipher()
    {
        var rsaN = new byte[64];
        Random.Shared.NextBytes(rsaN);
        rsaN[^1] = 0x7F;

        var (plain, cipher) = CryptoUtils.CreateEncryptedKey(rsaN);
        Assert.Equal(16, plain.Length);
        Assert.Equal(16, cipher.Length);
    }
}
