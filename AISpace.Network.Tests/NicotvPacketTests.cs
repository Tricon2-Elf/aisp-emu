using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Network.Tests;

public sealed class NicotvPacketTests
{
    [Fact]
    public void GetInfoByFurnitureRequest_ParsesFurnitureId()
    {
        var request = NicotvGetInfoByFurnitureRequest.FromBytes([2, 0, 0, 0]);

        Assert.Equal(2u, request.FurnitureId);
        Assert.Throws<InvalidDataException>(() =>
            NicotvGetInfoByFurnitureRequest.FromBytes([2, 0, 0])
        );
    }

    [Fact]
    public void OpenByFurnitureRequest_ParsesCapturedClientLayout()
    {
        var writer = new PacketWriter();
        writer.Write(2u);
        writer.Write(0u);
        writer.WriteFixedAsciiString("Hello World", NicotvData.MovieIdLength);
        writer.Write((uint)NicotvPlaybackState.Playing);
        writer.Write((uint)NicotvCommentVisibility.Visible);
        var payload = writer.ToBytes();

        Assert.Equal(NicotvOpenByFurnitureRequest.WireSize, payload.Length);

        var request = NicotvOpenByFurnitureRequest.FromBytes(payload);
        Assert.Equal(2u, request.FurnitureId);
        Assert.Equal(0u, request.Nicotv.ChannelId);
        Assert.Equal("Hello World", request.Nicotv.MovieId);
        Assert.Equal(NicotvPlaybackState.Playing, request.Nicotv.PlaybackState);
        Assert.Equal(NicotvCommentVisibility.Visible, request.Nicotv.CommentVisibility);
    }

    [Fact]
    public void GetInfoAndOpenResponses_UseFurnitureIdNicotvIdAndNicotvData()
    {
        var data = new NicotvData(
            3,
            "sm9",
            NicotvPlaybackState.Paused,
            NicotvCommentVisibility.Hidden
        );

        var getInfoPayload = new NicotvGetInfoByFurnitureResponse(2, 7, data).ToBytes();
        var openPayload = new NicotvOpenResponse(2, 7, data).ToBytes();

        Assert.Equal(sizeof(uint) * 2 + NicotvData.WireSize, getInfoPayload.Length);
        Assert.Equal(getInfoPayload, openPayload);

        var reader = new PacketReader(getInfoPayload);
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(7u, reader.ReadUInt());
        var decoded = NicotvData.FromBytes(getInfoPayload.AsSpan(sizeof(uint) * 2));
        Assert.Equal(3u, decoded.ChannelId);
        Assert.Equal("sm9", decoded.MovieId);
        Assert.Equal(NicotvPlaybackState.Paused, decoded.PlaybackState);
        Assert.Equal(NicotvCommentVisibility.Hidden, decoded.CommentVisibility);
    }

    [Fact]
    public void PlaybackControlPackets_UseDecompiledLayouts()
    {
        var getPlayhead = NicotvGetPlayheadTimeRequest.FromBytes(UIntPayload(1));
        Assert.Equal(1u, getPlayhead.NicotvId);
        Assert.Equal([1, 0, 0, 0, 42, 0, 0, 0], new NicotvGetPlayheadTimeResponse(1, 42).ToBytes());

        var setChannel = NicotvSetChannelRequest.FromBytes(UIntPayload(1, 1));
        Assert.Equal(1u, setChannel.NicotvId);
        Assert.Equal(1u, setChannel.ChannelId);
        Assert.Equal(UIntPayload(1, 1), new NicotvSetChannelResponse(1, 1).ToBytes());
        Assert.Equal(UIntPayload(1, 1), new NotifyNicotvSetChannel(1, 1).ToBytes());

        var close = NicotvCloseRequest.FromBytes(UIntPayload(1));
        Assert.Equal(1u, close.NicotvId);
        Assert.Equal(UIntPayload(0, 1), new NicotvCloseResponse(0, 1).ToBytes());
        Assert.Equal(UIntPayload(1), new NotifyNicotvClose(1).ToBytes());
    }

    [Fact]
    public void PlayheadPeerHandshake_UsesNicotvRequesterAndSeconds()
    {
        Assert.Equal(UIntPayload(1, 7), new NicotvGetPlayheadTimeRequestNotify(1, 7).ToBytes());

        var response = NicotvGetPlayheadTimeRequestRRequest.FromBytes(UIntPayload(1, 7, 42));
        Assert.Equal(1u, response.NicotvId);
        Assert.Equal(7u, response.RequestingUserId);
        Assert.Equal(42u, response.Seconds);
    }

    private static byte[] UIntPayload(params uint[] values)
    {
        var writer = new PacketWriter();
        foreach (var value in values)
            writer.Write(value);
        return writer.ToBytes();
    }
}
