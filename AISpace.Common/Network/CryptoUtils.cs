using System.Security.Cryptography;
using System.Text;
using System.Numerics;

namespace AISpace.Common.Network;

public class CryptoUtils
{

    private static readonly BigInteger RsaE = new(65537);

    private static BigInteger FromLeUnsigned(ReadOnlySpan<byte> le)
    {
        // Add a zero sign byte to force unsigned positive
        Span<byte> tmp = stackalloc byte[le.Length + 1];
        le.CopyTo(tmp);
        tmp[^1] = 0x00;

        return new BigInteger(tmp);
    }

    private static byte[] ToFixedLe(BigInteger x, int size)
    {
        byte[] le = x.ToByteArray(isUnsigned: true, isBigEndian: false);

        var result = new byte[size];
        le.CopyTo(result);
        return result;
    }

    private static byte[] CreatePlainKeyLe16(BigInteger n)
    {
        Span<byte> key = stackalloc byte[16];

        while (true)
        {
            RandomNumberGenerator.Fill(key);

            key[15] = 0; // force positive, matches original

            BigInteger m = FromLeUnsigned(key);
            if (m >= n)
                continue;

            return key.ToArray();
        }
    }

    public static (byte[] PlainKeyLe, byte[] EncryptedKeyLe) CreateEncryptedKey(byte[] rsaNLe)
    {
        var n = FromLeUnsigned(rsaNLe);
        var plainLe = CreatePlainKeyLe16(n);
        var m = FromLeUnsigned(plainLe);
        var c = BigInteger.ModPow(m, RsaE, n);
        var cipherLe = ToFixedLe(c, 16);

        return (plainLe, cipherLe);
    }

    public static string GenerateOTP()
    {
        byte[] seed = Guid.NewGuid().ToByteArray();
        byte[] hash = SHA256.HashData(seed);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        string opt = sb.ToString(0, 20);
        return opt;
    }

}
