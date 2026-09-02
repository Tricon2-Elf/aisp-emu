using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace aisp.Common.Tests;

public class AreaRoomListCloseHandlerTests
{
    [Fact]
    public async Task CloseWithRoomId_TeleportsVisitorToPublicRoom()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);

        db.Maps.Add(new Map { MapId = MyRoomInfo.SixTatamiMapId, Name = "MyRoom" });
        db.Channels.Add(
            new GameChannel
            {
                ChannelNum = 1,
                IP = "localhost",
                Port = 50054,
                MapId = 10990100,
            }
        );

        var visitorUser = new User { Username = "rl-visitor" };
        var visitor = new Character
        {
            Name = "Visitor",
            User = visitorUser,
            ModelId = 1,
            Birthdate = DateTime.UnixEpoch,
            CurrentMapId = MyRoomInfo.BaseMapId,
            HomeIslandId = 1,
        };
        var hostUser = new User { Username = "rl-host" };
        var host = new Character
        {
            Name = "Host",
            User = hostUser,
            ModelId = 1,
            Birthdate = DateTime.UnixEpoch,
            CurrentMapId = MyRoomInfo.BaseMapId,
            HomeIslandId = 1,
        };
        db.Users.AddRange(visitorUser, hostUser);
        db.Characters.AddRange(visitor, host);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var room = new Room
        {
            OwnerCharacterId = host.Id,
            Name = "Public Pad",
            Stage = MyRoomStage.SixTatami,
            Security = MyRoomSecurity.Public,
            IsDefault = true,
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var state = new SharedState();
        var session = new CapturingPlayerSession
        {
            MapId = MyRoomInfo.BaseMapId,
            ChannelId = 1,
            CharacterId = (uint)visitor.Id,
            User = visitorUser,
            Character = visitor,
        };
        visitorUser.Characters.Add(visitor);

        var handler = CreateHandler(db, state);
        var response = await handler.HandleAsync(
            new RoomListCloseRequest((uint)room.Id),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(response);
        Assert.Equal(0u, new PacketReader(response.ToBytes()).ReadUInt());
        Assert.Equal((uint)room.Id, session.MyRoomId);
        Assert.Contains(session.Sent, p => p.Type == PacketType.NotifyChangeMyRoom);
    }

    [Fact]
    public async Task CloseWithZero_DoesNotTeleport()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);
        var state = new SharedState();
        var handler = CreateHandler(db, state);
        var session = new CapturingPlayerSession { CharacterId = 1, MapId = MyRoomInfo.BaseMapId };

        var response = await handler.HandleAsync(
            new RoomListCloseRequest(0),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(response);
        Assert.Equal(0u, new PacketReader(response.ToBytes()).ReadUInt());
        Assert.Empty(session.Sent);
    }

    private static AreaRoomListCloseHandler CreateHandler(MainContext db, SharedState state) =>
        new(
            new MyRoomRepository(db),
            new DirectMapLinkTransitionService(
                new MapRepository(db),
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                new MyRoomRepository(db),
                new CircleRepository(db),
                new MapLinkRepository(db),
                new ChannelRepository(db),
                Options.Create(
                    new ServerOptions
                    {
                        NetworkOptions = new NetworkOptions(),
                        DbOptions = new DbOptions(),
                        IPOverride = "localhost",
                    }
                ),
                state,
                TestTextLocaliser.English,
                NullLogger<DirectMapLinkTransitionService>.Instance
            ),
            NullLogger<AreaRoomListCloseHandler>.Instance
        );
}
