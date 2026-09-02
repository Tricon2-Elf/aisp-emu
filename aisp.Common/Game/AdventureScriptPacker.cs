using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace aisp.Common.Game;

/// <summary>
/// The packed drama file the client caches as dl/drama/ai{scriptId}.txt and expects from download.php. Layout
/// (matches the official cache files and the community decoder): "ADV0", u32 1, u32 total length, u32 chardef
/// size, u32 drama size, u32 header size (20); then the actor table and the script, each UTF-16LE without BOM
/// plus four zero bytes, obfuscated by adding a 20-byte jammer (indexed from each section's start); the jammer
/// itself is the last 20 bytes of the file. The client builds this file itself from the datalist and contents
/// texts in the download.php XML (routine 0x4B1D10); the server only needs <see cref="Unpack"/> to import old
/// cache files, and <see cref="Pack"/> to test that.
/// </summary>
public static class AdventureScriptPacker
{
    public const int JammerLength = 20;
    private const int HeaderSize = 20;
    private const int TrailerLength = 4;
    private static readonly byte[] Signature = "ADV0"u8.ToArray();

    public static byte[] Pack(byte[] script, byte[] datalist, byte[]? jammer = null)
    {
        jammer ??= RandomNumberGenerator.GetBytes(JammerLength);
        if (jammer.Length != JammerLength)
            throw new ArgumentException("jammer must be 20 bytes", nameof(jammer));

        var chardef = ToUtf16(datalist);
        var drama = ToUtf16(script);
        var chardefSize = chardef.Length + TrailerLength;
        var dramaSize = drama.Length + TrailerLength;
        var total = 4 + HeaderSize + chardefSize + dramaSize + JammerLength;

        var blob = new byte[total];
        Signature.CopyTo(blob, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(8), (uint)total);
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(12), (uint)chardefSize);
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(16), (uint)dramaSize);
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(20), HeaderSize);

        var offset = 4 + HeaderSize;
        Obfuscate(chardef, blob, offset, jammer);
        offset += chardefSize;
        Obfuscate(drama, blob, offset, jammer);
        offset += dramaSize;
        jammer.CopyTo(blob, offset);
        return blob;
    }

    /// <summary>Reverses <see cref="Pack"/>; returns the two payloads as UTF-16LE text without BOM. Null when the blob is not a drama pack.</summary>
    public static (byte[] Script, byte[] Datalist)? Unpack(byte[] blob)
    {
        if (
            blob.Length < 4 + HeaderSize + JammerLength
            || !blob.AsSpan(0, 4).SequenceEqual(Signature)
        )
            return null;
        long chardefSize = BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(12));
        long dramaSize = BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(16));
        long headerSize = BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(20));
        var chardefStart = 4 + headerSize;
        var dramaStart = chardefStart + chardefSize;
        if (
            headerSize < HeaderSize
            || chardefSize < TrailerLength
            || dramaSize < TrailerLength
            || dramaStart + dramaSize + JammerLength > blob.Length
        )
            return null;
        var jammer = blob.AsSpan(blob.Length - JammerLength);
        return (
            Deobfuscate(blob, (int)dramaStart, (int)(dramaSize - TrailerLength), jammer),
            Deobfuscate(blob, (int)chardefStart, (int)(chardefSize - TrailerLength), jammer)
        );
    }

    /// <summary>UTF-8 (with or without BOM) or UTF-16LE with BOM in; UTF-16LE without BOM out.</summary>
    public static byte[] ToUtf16(byte[] text)
    {
        if (text.Length >= 2 && text[0] == 0xFF && text[1] == 0xFE)
            return text[2..];
        return Encoding.Unicode.GetBytes(Encoding.UTF8.GetString(StripUtf8Bom(text)));
    }

    /// <summary>UTF-16LE (with or without BOM) in; UTF-8 without BOM out, the form every listing stores.</summary>
    public static byte[] ToUtf8(byte[] utf16)
    {
        var body =
            utf16.Length >= 2 && utf16[0] == 0xFF && utf16[1] == 0xFE ? utf16.AsSpan(2) : utf16;
        return Encoding.UTF8.GetBytes(Encoding.Unicode.GetString(body));
    }

    public static ReadOnlySpan<byte> StripUtf8Bom(byte[] text) =>
        text.Length >= 3 && text[0] == 0xEF && text[1] == 0xBB && text[2] == 0xBF
            ? text.AsSpan(3)
            : text.AsSpan();

    private static void Obfuscate(byte[] plain, byte[] target, int offset, byte[] jammer)
    {
        for (var i = 0; i < plain.Length; i++)
            target[offset + i] = (byte)(plain[i] + jammer[i % JammerLength]);
        for (var i = plain.Length; i < plain.Length + TrailerLength; i++)
            target[offset + i] = jammer[i % JammerLength];
    }

    private static byte[] Deobfuscate(byte[] blob, int start, int length, ReadOnlySpan<byte> jammer)
    {
        var plain = new byte[length];
        for (var i = 0; i < length; i++)
            plain[i] = (byte)(blob[start + i] - jammer[i % JammerLength]);
        return plain;
    }
}
