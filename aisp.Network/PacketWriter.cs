using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace aisp.Network;

public class PacketWriter : IPacketWriter
{
    private readonly MemoryStream _stream = new();

    public byte[] ToBytes() => _stream.ToArray();

    public void Reset()
    {
        _stream.SetLength(0);
        _stream.Position = 0;
    }

    public ReadOnlyMemory<byte> WrittenMemory
    {
        get
        {
            _stream.TryGetBuffer(out var segment);
            return segment.AsMemory(0, (int)_stream.Length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteLE<T>(T value, Action<Span<byte>, T> write)
    {
        Span<byte> span = stackalloc byte[Unsafe.SizeOf<T>()];
        write(span, value);
        _stream.Write(span);
    }

    public void Write(ushort value) => WriteLE(value, BinaryPrimitives.WriteUInt16LittleEndian);

    public void Write(short value) => WriteLE(value, BinaryPrimitives.WriteInt16LittleEndian);

    public void Write(ulong value) => WriteLE(value, BinaryPrimitives.WriteUInt64LittleEndian);

    public void Write(float value) => WriteLE(value, BinaryPrimitives.WriteSingleLittleEndian);

    public void Write(uint value) => WriteLE(value, BinaryPrimitives.WriteUInt32LittleEndian);

    public void Write(int value) => WriteLE(value, BinaryPrimitives.WriteInt32LittleEndian);

    public void Write(byte value) => _stream.WriteByte(value);

    public void Write(sbyte value) => _stream.WriteByte((byte)value);

    public void Write(ReadOnlySpan<byte> source) => _stream.Write(source);

    public void Write(string value, string encoderName = "utf-8") =>
        WriteNullTerminated(value, encoderName, maxBytes: int.MaxValue);

    public void Write(string value, int maxBytes, string encoderName = "utf-8") =>
        WriteNullTerminated(value, encoderName, maxBytes);

    public void WriteFixedString(string value, int length, string encoderName = "utf-8")
    {
        var encoder = PacketEncoding.GetEncoding(encoderName);
        Span<byte> buffer = stackalloc byte[length];
        buffer.Clear();
        encoder.GetBytes(TruncateToBytes(value, length, encoder), buffer);
        _stream.Write(buffer);
    }

    public void WriteFixedJisString(string value, int length) =>
        WriteFixedString(value, length, "Shift_JIS");

    public void WriteFixedAsciiString(string value, int length) =>
        WriteFixedString(value, length, "ASCII");

    private void WriteNullTerminated(string value, string encoderName, int maxBytes)
    {
        var encoder = PacketEncoding.GetEncoding(encoderName);
        value = TruncateToBytes(value, maxBytes, encoder);
        var size = encoder.GetByteCount(value);
        Span<byte> buffer = stackalloc byte[size + 1];
        encoder.GetBytes(value, buffer);
        buffer[size] = 0x00;
        _stream.Write(buffer);
    }

    private static string TruncateToBytes(string value, int maxBytes, Encoding encoding)
    {
        if (maxBytes <= 0)
            return string.Empty;
        if (encoding.GetByteCount(value) <= maxBytes)
            return value;

        var length = value.Length;
        while (length > 0 && encoding.GetByteCount(value.AsSpan(0, length)) > maxBytes)
            length--;
        return value[..length];
    }
}
