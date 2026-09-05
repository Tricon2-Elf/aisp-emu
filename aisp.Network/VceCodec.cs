using System.Buffers.Binary;

namespace aisp.Network;

/// <summary>
/// VCE PacketData encoding and frame packing for outbound sends.
/// </summary>
public static class VceCodec
{
    /// <summary>Max plaintext size per encrypted wire frame (Camellia chunk).</summary>
    public const int MaxChunkSize = 1392;

    /// <summary>PacketData codec byte with 4-byte length field (0x0 | 0x3).</summary>
    public const byte PacketDataHeaderPrefix = 0x03;

    public const int PacketTypeSize = 2;
    public const int PacketHeaderSize = 1 + sizeof(uint) + PacketTypeSize;

    public static byte[] EncodePacketData(PacketType type, ReadOnlySpan<byte> payload)
    {
        var packet = new byte[PacketHeaderSize + payload.Length];
        WritePacketData(packet, type, payload);
        return packet;
    }

    /// <summary>
    /// Encodes complete PacketData messages into plaintext frames of at most
    /// <paramref name="maxFrameSize"/>. Oversized messages are emitted alone so the
    /// connection can apply its existing encrypted chunking.
    /// </summary>
    public static List<byte[]> EncodePacketDataFrames(
        IReadOnlyList<(PacketType Type, byte[] Payload)> packets,
        int maxFrameSize = MaxChunkSize
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFrameSize, PacketHeaderSize);

        var frames = new List<byte[]>();
        for (var start = 0; start < packets.Count;)
        {
            var end = start;
            var frameSize = 0;
            while (end < packets.Count)
            {
                var packetSize = PacketHeaderSize + packets[end].Payload.Length;
                if (packetSize > maxFrameSize)
                {
                    if (end == start)
                        end++;
                    break;
                }

                if (frameSize + packetSize > maxFrameSize)
                    break;

                frameSize += packetSize;
                end++;
            }

            if (frameSize == 0)
            {
                frames.Add(EncodePacketData(packets[start].Type, packets[start].Payload));
                start = end;
                continue;
            }

            var frame = new byte[frameSize];
            var offset = 0;
            for (var i = start; i < end; i++)
            {
                var (type, payload) = packets[i];
                WritePacketData(frame.AsSpan(offset), type, payload);
                offset += PacketHeaderSize + payload.Length;
            }

            frames.Add(frame);
            start = end;
        }

        return frames;
    }

    private static void WritePacketData(Span<byte> destination, PacketType type, ReadOnlySpan<byte> payload)
    {
        destination[0] = PacketDataHeaderPrefix;
        BinaryPrimitives.WriteUInt32LittleEndian(
            destination[1..],
            (uint)(payload.Length + PacketTypeSize)
        );
        BinaryPrimitives.WriteUInt16LittleEndian(destination[5..], (ushort)type);
        payload.CopyTo(destination[PacketHeaderSize..]);
    }
}
