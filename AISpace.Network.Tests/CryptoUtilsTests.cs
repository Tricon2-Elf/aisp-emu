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
        var rsaN = ValidTestModulus();

        var (plain, cipher) = CryptoUtils.CreateEncryptedKey(rsaN);
        Assert.Equal(16, plain.Length);
        Assert.Equal(16, cipher.Length);
    }

    [Fact]
    public void IsPlausibleClientRsaModulus_AcceptsClientSizedModulus()
    {
        Assert.True(CryptoUtils.IsPlausibleClientRsaModulus(ValidTestModulus()));
    }

    [Theory]
    [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })]
    [InlineData(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })]
    [InlineData(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x40 })]
    public void IsPlausibleClientRsaModulus_RejectsScannerGarbage(byte[] rsaN)
    {
        Assert.False(CryptoUtils.IsPlausibleClientRsaModulus(rsaN));
    }

    private static byte[] ValidTestModulus()
    {
        var rsaN = new byte[16];
        rsaN[15] = 0x40; // ~2^126, within real client range
        rsaN[0] = 0x03;
        return rsaN;
    }
}
