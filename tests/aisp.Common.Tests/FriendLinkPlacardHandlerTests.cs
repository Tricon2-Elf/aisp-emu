using System.Numerics;
using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Handlers.Msg;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class FriendLinkPlacardHandlerTests
{
    [Fact]
    public async Task PlacementReplayCommentsAndAreaExit_WorkEndToEndInActiveState()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 10, ct);
        await TestDb.SeedCharacterAsync(options, 20, ct);

        await using var db = new MainContext(options);
        var friends = new FriendRepository(db);
        Assert.Equal(FriendResult.Ok, await friends.SetLinkTagAsync(10, 0, "Anime", ct));

        var ownerCharacter = await db
            .Characters.Include(x => x.User)
            .SingleAsync(x => x.Id == 10, ct);
        var visitorCharacter = await db
            .Characters.Include(x => x.User)
            .SingleAsync(x => x.Id == 20, ct);
        var ownerArea = CreateSession(ownerCharacter, mapId: 7, channelId: 2);
        var visitorArea = CreateSession(visitorCharacter, mapId: 7, channelId: 2);
        var state = new SharedState();
        state.RegisterClient(ServerType.Area, ownerArea);
        state.RegisterClient(ServerType.Area, visitorArea);

        await PlaceAsync(friends, state, ownerArea, ct);

        Assert.Collection(
            ownerArea.Sent,
            response => Assert.Equal(PacketType.PlacardSettingResponse, response.Type),
            notify => Assert.Equal(PacketType.NotifyPlacardSetting, notify.Type)
        );
        var liveNotify = Assert.Single(
            visitorArea.Sent,
            x => x.Type == PacketType.NotifyPlacardSetting
        );
        var placard = Assert.Single(state.GetFriendLinkPlacards(7, 2, 0));
        AssertPlacard(liveNotify.Payload, placard.PlacardId, hasCount: false);
        Assert.Equal(new Vector3(1.25f, 2.5f, 3.75f), placard.Position);

        var joining = CreateSession(visitorCharacter, mapId: 7, channelId: 2);
        await new AreaMapDataEnterEndHandler(
            NullLogger<AreaMapDataEnterEndHandler>.Instance,
            null,
            state
        ).HandleAsync(ReadOnlyMemory<byte>.Empty, joining, ct);
        var replay = Assert.Single(joining.Sent, x => x.Type == PacketType.NotifyPlacardInMap);
        AssertPlacard(replay.Payload, placard.PlacardId, hasCount: true);

        var ownerMsg = CreateSession(ownerCharacter);
        ownerMsg.CharacterId = 0;
        var visitorMsg = CreateSession(visitorCharacter);
        state.RegisterClient(ServerType.Msg, ownerMsg);
        state.RegisterClient(ServerType.Msg, visitorMsg);

        var logRequest = new PacketWriter();
        logRequest.Write(placard.PlacardId);
        await new GetPlacardCommentLogHandler(state).HandleAsync(
            logRequest.ToBytes(),
            visitorMsg,
            ct
        );
        visitorMsg.Sent.Clear();

        var commentRequest = new PacketWriter();
        commentRequest.Write(5u);
        commentRequest.Write(0u);
        commentRequest.Write("Nice placard!");
        commentRequest.Write(0u);
        await new PostTalkHandler(
            state,
            WordFilter.FromTerms([]),
            TestTextLocaliser.English,
            new CapturingChatLog()
        ).HandleAsync(commentRequest.ToBytes(), visitorMsg, ct);

        Assert.Equal("Nice placard!", Assert.Single(placard.GetComments()).Message);
        var ownerNotification = Assert.Single(
            ownerMsg.Sent,
            x => x.Type == PacketType.NotifyPlacardCommentLog
        );
        var commentReader = new PacketReader(ownerNotification.Payload);
        Assert.Equal(0u, commentReader.ReadUInt());
        Assert.Equal(placard.PlacardId, commentReader.ReadUInt());
        Assert.Equal(1u, commentReader.ReadUInt());
        Assert.Equal("character-20", commentReader.ReadFixedString(37));
        Assert.Equal("Nice placard!", commentReader.ReadFixedString(385));

        visitorArea.Sent.Clear();
        await state.BroadcastAreaDisappearAsync(ownerArea, ct);
        Assert.Empty(state.GetFriendLinkPlacards(7, 2, 0));
        var remove = Assert.Single(visitorArea.Sent, x => x.Type == PacketType.NotifyPlacardRemove);
        Assert.Equal(placard.PlacardId, new PacketReader(remove.Payload).ReadUInt());
        Assert.Empty(placard.GetComments());

        visitorArea.Sent.Clear();
        await PlaceAsync(friends, state, ownerArea, ct);
        var replacement = Assert.Single(state.GetFriendLinkPlacards(7, 2, 0));
        visitorArea.Sent.Clear();
        await ((IPacketHandler)new AreaPlacardRemoveHandler(state)).HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            ownerArea,
            ct
        );
        Assert.Empty(state.GetFriendLinkPlacards(7, 2, 0));
        var explicitRemove = Assert.Single(
            visitorArea.Sent,
            x => x.Type == PacketType.NotifyPlacardRemove
        );
        Assert.Equal(replacement.PlacardId, new PacketReader(explicitRemove.Payload).ReadUInt());
    }

    [Fact]
    public void Disconnect_RemovesActivePlacard()
    {
        var state = new SharedState();
        var owner = new CapturingPlayerSession
        {
            UserId = 10,
            CharacterId = 10,
            MapId = 7,
            ChannelId = 2,
        };
        var viewer = new CapturingPlayerSession
        {
            UserId = 20,
            CharacterId = 20,
            MapId = 7,
            ChannelId = 2,
        };
        state.RegisterClient(ServerType.Area, owner);
        state.RegisterClient(ServerType.Area, viewer);
        var (placard, _) = state.SetFriendLinkPlacard(
            10,
            10,
            "Owner",
            7,
            2,
            0,
            0,
            1,
            0,
            0,
            "Anime",
            default
        );

        state.UnregisterClient(ServerType.Area, owner.ConnectionId);

        Assert.Empty(state.GetFriendLinkPlacards(7, 2, 0));
        var remove = Assert.Single(viewer.Sent, x => x.Type == PacketType.NotifyPlacardRemove);
        Assert.Equal(placard.PlacardId, new PacketReader(remove.Payload).ReadUInt());
    }

    private static async Task PlaceAsync(
        IFriendRepository friends,
        SharedState state,
        IPlayerSession owner,
        CancellationToken ct
    )
    {
        var payload = new PacketWriter();
        payload.Write(0u);
        payload.Write(0u);
        payload.Write(1.25f);
        payload.Write(2.5f);
        payload.Write(3.75f);
        payload.Write((byte)4);
        await ((IPacketHandler)new AreaPlacardSettingHandler(friends, state)).HandleAsync(
            payload.ToBytes(),
            owner,
            ct
        );
    }

    private static CapturingPlayerSession CreateSession(
        aisp.Common.DAL.Entities.Character character,
        uint mapId = 0,
        int channelId = 0
    ) =>
        new()
        {
            UserId = character.UserId,
            User = character.User,
            CharacterId = checked((uint)character.Id),
            Character = character,
            MapId = mapId,
            ChannelId = channelId,
        };

    private static void AssertPlacard(byte[] payload, uint placardId, bool hasCount)
    {
        var reader = new PacketReader(payload);
        if (hasCount)
            Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(placardId, reader.ReadUInt());
        Assert.Equal("character-10", reader.ReadFixedString(37));
        Assert.Equal(1.25f, reader.ReadFloat());
        Assert.Equal(2.5f, reader.ReadFloat());
        Assert.Equal(3.75f, reader.ReadFloat());
        Assert.Equal((byte)4, reader.ReadByte());
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal("Anime", reader.ReadFixedString(61));
        Assert.Equal(0u, reader.ReadUInt());
    }
}
