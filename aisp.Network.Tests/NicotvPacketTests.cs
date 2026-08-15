using System.Reflection;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public sealed class NicotvPacketTests
{
    [Fact]
    public void NicoLiveReloadPackets_UseDecompiledOpcodesAndLayouts()
    {
        Assert.Equal(0x5D63, (ushort)PacketType.NicoliveReloadRequest);
        Assert.Equal(0xE342, (ushort)PacketType.NotifyNicoliveReload);

        Assert.Equal(
            "send_nicolive_reload",
            GetMetadata(PacketType.NicoliveReloadRequest).DecompiledName
        );
        Assert.Equal(
            "recv_notify_nicolive_reload",
            GetMetadata(PacketType.NotifyNicoliveReload).DecompiledName
        );

        Assert.NotNull(NicoliveReloadRequest.FromBytes([]));
        Assert.Throws<InvalidDataException>(() => NicoliveReloadRequest.FromBytes([0]));
        Assert.Equal("lv123\0"u8.ToArray(), new NotifyNicoliveReload("lv123").ToBytes());
    }

    [Fact]
    public void NicoLiveReloadNotification_RejectsValuesTheClientCannotDecode()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new NotifyNicoliveReload("日本語").ToBytes()
        );
        Assert.Throws<InvalidOperationException>(() =>
            new NotifyNicoliveReload(
                new string('a', NotifyNicoliveReload.MaximumEncodedLiveIdBytes + 1)
            ).ToBytes()
        );
    }

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

        var play = NicotvPlayRequest.FromBytes(UIntPayload(1, (uint)NicotvPlaybackState.Playing));
        Assert.Equal(1u, play.NicotvId);
        Assert.Equal((uint)NicotvPlaybackState.Playing, play.Status);
        Assert.Equal(UIntPayload(0, 1), new NicotvPlayResponse(0, 1).ToBytes());
        Assert.Equal(
            UIntPayload(1, (uint)NicotvPlaybackState.Playing),
            new NotifyNicotvPlay(1, (uint)NicotvPlaybackState.Playing).ToBytes()
        );

        var movieWriter = new PacketWriter();
        movieWriter.Write(1u);
        movieWriter.Write("sm9"u8);
        movieWriter.Write((byte)0);
        var setMovie = NicotvSetMovieRequest.FromBytes(movieWriter.ToBytes());
        Assert.Equal(1u, setMovie.NicotvId);
        Assert.Equal("sm9", setMovie.MovieId);
        Assert.Equal(UIntPayload(0, 1), new NicotvSetMovieResponse(0, 1).ToBytes());
        Assert.Equal(movieWriter.ToBytes(), new NotifyNicotvSetMovie(1, "sm9").ToBytes());

        Assert.Equal(0x0001, (ushort)PacketType.NicotvCloseResponse);
        Assert.Equal(0x90B9, (ushort)PacketType.NicotvPlayRequest);
        Assert.Equal(0xDDCA, (ushort)PacketType.NicotvSetMovieRequest);
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

    private static PacketMetadata GetMetadata(PacketType packetType)
    {
        var field = typeof(PacketType).GetField(packetType.ToString());
        return Assert.IsType<PacketMetadata>(field?.GetCustomAttribute<PacketMetadata>());
    }
}
