using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace AISpace.Network;

public ref struct PacketReader(ReadOnlySpan<byte> buffer)
{
    private readonly ReadOnlySpan<byte> _buffer = buffer;
    private int _offset = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadSpan(int length)
    {
        if (_offset + length > _buffer.Length)
            throw new EndOfStreamException($"PacketReader: tried to read {length} bytes with {_buffer.Length - _offset} remaining");

        var span = _buffer.Slice(_offset, length);
        _offset += length;
        return span;
    }

    public byte ReadByte() => ReadSpan(1)[0];

    public sbyte ReadSByte() => (sbyte)ReadSpan(1)[0];

    public float ReadFloat() => BinaryPrimitives.ReadSingleLittleEndian(ReadSpan(sizeof(float)));

    public ushort ReadUShort() => BinaryPrimitives.ReadUInt16LittleEndian(ReadSpan(sizeof(ushort)));

    public uint ReadUInt() => BinaryPrimitives.ReadUInt32LittleEndian(ReadSpan(sizeof(uint)));

    public ulong ReadULong() => BinaryPrimitives.ReadUInt64LittleEndian(ReadSpan(sizeof(ulong)));

    public ReadOnlySpan<byte> ReadBytes(int count) => ReadSpan(count);

    public string ReadFixedString(int length, string encoderName = "UTF8")
    {
        var encoder = PacketEncoding.GetEncoding(encoderName);
        var span = ReadSpan(length);

        // Trim at first null if present
        int nullIndex = span.IndexOf((byte)0x00);
        if (nullIndex >= 0)
            span = span[..nullIndex];

        return encoder.GetString(span);
    }

    public string ReadString(string encoderName = "ASCII")
    {
        var encoder = PacketEncoding.GetEncoding(encoderName);
        var remaining = _buffer[_offset..];
        int nullIndex = remaining.IndexOf((byte)0x00);

        if (nullIndex < 0)
        {
            _offset = _buffer.Length;
            return encoder.GetString(remaining);
        }

        var span = remaining[..nullIndex];
        _offset += nullIndex + 1;

        return encoder.GetString(span);
    }
}
