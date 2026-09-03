namespace aisp.Network.Tests;

public class VceCodecTests
{
    [Fact]
    public void EncodePacketData_UsesFourByteLengthPrefix()
    {
        var payload = new byte[] { 0xAA, 0xBB };
        var encoded = VceCodec.EncodePacketData(PacketType.TalkForwardNotify, payload);

        Assert.Equal(0x03, encoded[0]);
        Assert.Equal(
            (uint)(payload.Length + VceCodec.PacketTypeSize),
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(encoded.AsSpan(1, 4))
        );
        Assert.Equal(
            (ushort)PacketType.TalkForwardNotify,
            System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(encoded.AsSpan(5, 2))
        );
        Assert.Equal(payload, encoded.AsSpan(7).ToArray());
    }

    [Fact]
    public void EncodePacketDataFrames_FitsManySmallPacketsIntoOneFrame()
    {
        var packets = new List<(PacketType Type, byte[] Payload)>();
        for (var i = 0; i < 30; i++)
            packets.Add((PacketType.ItemCreateNotify, new byte[22]));

        var frames = VceCodec.EncodePacketDataFrames(packets);

        Assert.Single(frames);
        Assert.True(frames[0].Length < VceCodec.MaxChunkSize);
        Assert.Equal(
            packets.Sum(p => VceCodec.PacketHeaderSize + p.Payload.Length),
            frames[0].Length
        );
    }

    [Fact]
    public void EncodePacketDataFrames_SplitsWhenNextPacketWouldExceedMax()
    {
        // Each encoded packet is 7 + 700 = 707 bytes; two fit (1414 > 1392), so one per frame.
        var a = (PacketType.TalkForwardNotify, new byte[700]);
        var b = (PacketType.TalkForwardNotify, new byte[700]);
        var encodedA = VceCodec.EncodePacketData(a.Item1, a.Item2);
        var encodedB = VceCodec.EncodePacketData(b.Item1, b.Item2);
        Assert.True(encodedA.Length + encodedB.Length > VceCodec.MaxChunkSize);

        var frames = VceCodec.EncodePacketDataFrames([a, b]);

        Assert.Equal(2, frames.Count);
        Assert.Equal(encodedA, frames[0]);
        Assert.Equal(encodedB, frames[1]);
    }

    [Fact]
    public void EncodePacketDataFrames_EmitsOversizedPacketAloneAfterFlushingCurrent()
    {
        var small = (PacketType.TalkForwardNotify, new byte[10]);
        var oversized = (
            PacketType.ItemGetBaseListResponse,
            new byte[VceCodec.MaxChunkSize]
        );
        var encodedSmall = VceCodec.EncodePacketData(small.Item1, small.Item2);
        var encodedOversized = VceCodec.EncodePacketData(oversized.Item1, oversized.Item2);
        Assert.True(encodedOversized.Length > VceCodec.MaxChunkSize);

        var frames = VceCodec.EncodePacketDataFrames([small, oversized, small]);

        Assert.Equal(3, frames.Count);
        Assert.Equal(encodedSmall, frames[0]);
        Assert.Equal(encodedOversized, frames[1]);
        Assert.Equal(encodedSmall, frames[2]);
    }

    [Fact]
    public void EncodePacketDataFrames_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(VceCodec.EncodePacketDataFrames([]));
    }
}
