namespace aisp.Network;

public interface IPacketWriter
{
    byte[] ToBytes();

    void Reset();

    ReadOnlyMemory<byte> WrittenMemory { get; }

    void Write(ushort value);

    void Write(short value);

    void Write(ulong value);

    void Write(float value);

    void Write(uint value);

    void Write(int value);

    void Write(byte value);

    void Write(sbyte value);

    void Write(ReadOnlySpan<byte> source);

    void Write(string value, string encoderName = "ASCII");

    void WriteFixedString(string value, int length, string encoderName = "Shift_JIS");

    void WriteFixedJisString(string value, int length);

    void WriteFixedAsciiString(string value, int length);
}
