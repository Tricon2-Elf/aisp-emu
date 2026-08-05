using AISpace.Common.Config;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Tests;

public sealed class AreaNicotvHandlersTests
{
    [Fact]
    public async Task NicoLiveReload_ReturnsConfiguredLiveId()
    {
        var options = Options.Create(
            new ServerOptions { NicoLive = new NicoLiveOptions { LiveId = " lv123 " } }
        );
        var handler = new AreaNicoliveReloadHandler(
            options,
            NullLogger<AreaNicoliveReloadHandler>.Instance
        );
        var session = CreateVisitorSession();

        await ((IPacketHandler)handler).HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        var response = Assert.Single(session.Sent);
        Assert.Equal(PacketType.NotifyNicoliveReload, response.Type);
        Assert.Equal("lv123\0"u8.ToArray(), response.Payload);
    }

    [Fact]
    public async Task GetInfoAndOpen_PersistNicotvStateForPlacedFurniture()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await SeedNicotvFurnitureAsync(options, ct);

            uint nicotvId;
            await using (var db = new MainContext(options))
            {
                var repository = new NicotvRepository(db);
                var session = CreateVisitorSession();
                var getInfoHandler = new AreaNicotvGetInfoByFurnitureHandler(repository);

                await ((IPacketHandler)getInfoHandler).HandleAsync(
                    BuildGetInfoPayload(2),
                    session,
                    ct
                );

                var response = Assert.Single(session.Sent);
                Assert.Equal(PacketType.NicotvGetInfoByFurnitureResponse, response.Type);
                var reader = new PacketReader(response.Payload);
                Assert.Equal(2u, reader.ReadUInt());
                nicotvId = reader.ReadUInt();
                Assert.NotEqual(0u, nicotvId);
                var initial = NicotvData.FromBytes(response.Payload.AsSpan(sizeof(uint) * 2));
                Assert.Equal("", initial.MovieId);
                Assert.Equal(NicotvPlaybackState.Closed, initial.PlaybackState);

                session.Sent.Clear();
                var openHandler = new AreaNicotvOpenByFurnitureHandler(
                    repository,
                    new SharedState()
                );
                await ((IPacketHandler)openHandler).HandleAsync(
                    BuildOpenPayload(
                        2,
                        new NicotvData(
                            0,
                            "Hello World",
                            NicotvPlaybackState.Playing,
                            NicotvCommentVisibility.Visible
                        )
                    ),
                    session,
                    ct
                );

                response = Assert.Single(session.Sent);
                Assert.Equal(PacketType.NicotvOpenResponse, response.Type);
                reader = new PacketReader(response.Payload);
                Assert.Equal(2u, reader.ReadUInt());
                Assert.Equal(nicotvId, reader.ReadUInt());
                var opened = NicotvData.FromBytes(response.Payload.AsSpan(sizeof(uint) * 2));
                Assert.Equal("Hello World", opened.MovieId);
                Assert.Equal(NicotvPlaybackState.Playing, opened.PlaybackState);
            }

            await using (var db = new MainContext(options))
            {
                var stored = await db.Nicotvs.SingleAsync(entry => entry.Id == nicotvId, ct);
                Assert.Equal(42, stored.RoomId);
                Assert.Equal(2u, stored.FurnitureId);
                Assert.Equal("Hello World", stored.MovieId);
                Assert.Equal(NicotvPlaybackState.Playing, stored.PlaybackState);

                var repository = new NicotvRepository(db);
                var session = CreateVisitorSession();
                var handler = new AreaNicotvGetInfoByFurnitureHandler(repository);
                await ((IPacketHandler)handler).HandleAsync(BuildGetInfoPayload(2), session, ct);

                var response = Assert.Single(session.Sent);
                var reader = new PacketReader(response.Payload);
                Assert.Equal(2u, reader.ReadUInt());
                Assert.Equal(nicotvId, reader.ReadUInt());
                Assert.Equal(
                    "Hello World",
                    NicotvData.FromBytes(response.Payload.AsSpan(sizeof(uint) * 2)).MovieId
                );
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetInfo_RejectsFurnitureOutsideTheCurrentRoom()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await SeedNicotvFurnitureAsync(options, ct);

            await using var db = new MainContext(options);
            var session = CreateVisitorSession();
            var handler = new AreaNicotvGetInfoByFurnitureHandler(new NicotvRepository(db));
            await ((IPacketHandler)handler).HandleAsync(BuildGetInfoPayload(999), session, ct);

            var response = Assert.Single(session.Sent);
            var reader = new PacketReader(response.Payload);
            Assert.Equal(999u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Empty(await db.Nicotvs.ToListAsync(ct));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task RemovingFurniture_CascadesItsNicotvState()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await SeedNicotvFurnitureAsync(options, ct);

            await using var db = new MainContext(options);
            var repository = new NicotvRepository(db);
            Assert.NotNull(await repository.GetOrCreateForFurnitureAsync(42, 2, ct));

            db.MyRoomFurniture.Remove(
                await db.MyRoomFurniture.SingleAsync(
                    entry => entry.RoomId == 42 && entry.FurnitureId == 2,
                    ct
                )
            );
            await db.SaveChangesAsync(ct);

            Assert.Empty(await db.Nicotvs.ToListAsync(ct));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SetChannelAndClose_PersistAndNotifyOtherRoomOccupants()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await SeedNicotvFurnitureAsync(options, ct);

            await using var db = new MainContext(options);
            var repository = new NicotvRepository(db);
            var nicotv = Assert.IsType<Nicotv>(
                await repository.UpdateForFurnitureAsync(
                    42,
                    2,
                    new NicotvData(playbackState: NicotvPlaybackState.Playing),
                    ct
                )
            );
            var nicotvId = checked((uint)nicotv.Id);

            var state = new SharedState();
            var actor = CreateVisitorSession(2);
            var peer = CreateVisitorSession(3);
            state.RegisterClient(ServerType.Area, actor);
            state.RegisterClient(ServerType.Area, peer);

            var setChannelHandler = new AreaNicotvSetChannelHandler(repository, state);
            await ((IPacketHandler)setChannelHandler).HandleAsync(
                BuildUIntPayload(nicotvId, 1),
                actor,
                ct
            );

            var response = Assert.Single(actor.Sent);
            Assert.Equal(PacketType.NicotvSetChannelResponse, response.Type);
            Assert.Equal(BuildUIntPayload(nicotvId, 1), response.Payload);
            var notification = Assert.Single(peer.Sent);
            Assert.Equal(PacketType.NotifyNicotvSetChannel, notification.Type);
            Assert.Equal(BuildUIntPayload(nicotvId, 1), notification.Payload);
            Assert.Equal(1u, nicotv.ChannelId);

            actor.Sent.Clear();
            peer.Sent.Clear();
            var closeHandler = new AreaNicotvCloseHandler(repository, state);
            await ((IPacketHandler)closeHandler).HandleAsync(BuildUIntPayload(nicotvId), actor, ct);

            response = Assert.Single(actor.Sent);
            Assert.Equal(PacketType.NicotvCloseResponse, response.Type);
            Assert.Equal(BuildUIntPayload(0, nicotvId), response.Payload);
            notification = Assert.Single(peer.Sent);
            Assert.Equal(PacketType.NotifyNicotvClose, notification.Type);
            Assert.Equal(BuildUIntPayload(nicotvId), notification.Payload);
            Assert.Equal(NicotvPlaybackState.Closed, nicotv.PlaybackState);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetPlayheadTime_RoundTripsThroughAnotherRoomOccupant()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await SeedNicotvFurnitureAsync(options, ct);

            await using var db = new MainContext(options);
            var repository = new NicotvRepository(db);
            var nicotv = Assert.IsType<Nicotv>(
                await repository.GetOrCreateForFurnitureAsync(42, 2, ct)
            );
            var nicotvId = checked((uint)nicotv.Id);

            var state = new SharedState();
            var requester = CreateVisitorSession(2);
            var peer = CreateVisitorSession(3);
            state.RegisterClient(ServerType.Area, requester);
            state.RegisterClient(ServerType.Area, peer);

            var requestHandler = new AreaNicotvGetPlayheadTimeHandler(repository, state);
            await ((IPacketHandler)requestHandler).HandleAsync(
                BuildUIntPayload(nicotvId),
                requester,
                ct
            );

            Assert.Empty(requester.Sent);
            var notification = Assert.Single(peer.Sent);
            Assert.Equal(PacketType.NicotvGetPlayheadTimeRequestNotify, notification.Type);
            Assert.Equal(BuildUIntPayload(nicotvId, 2), notification.Payload);

            var answerHandler = new AreaNicotvGetPlayheadTimeRequestRHandler(repository, state);
            Assert.Same(requester, state.GetAreaSessionByUserId(2, peer.MapId, peer.ChannelId));
            await answerHandler.HandleAsync(BuildUIntPayload(nicotvId, 2, 37), peer, ct);

            var response = Assert.Single(
                requester.Sent,
                packet => packet.Type == PacketType.NicotvGetPlayheadTimeResponse
            );
            Assert.Equal(BuildUIntPayload(nicotvId, 37), response.Payload);
            Assert.Contains(
                requester.Sent,
                packet => packet.Type == PacketType.NotifyNicotvSetPlayheadTime
            );
            Assert.Contains(
                peer.Sent,
                packet =>
                    packet.Type == PacketType.NotifyNicotvSetPlayheadTime
                    && packet.Payload.SequenceEqual(BuildUIntPayload(nicotvId, 37))
            );
            Assert.Contains(
                peer.Sent,
                packet => packet.Type == PacketType.NicotvSetPlayheadTimeResponse
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PlayAndSetMovie_PersistAndNotifyOtherRoomOccupants()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await SeedNicotvFurnitureAsync(options, ct);

            await using var db = new MainContext(options);
            var repository = new NicotvRepository(db);
            var nicotv = Assert.IsType<Nicotv>(
                await repository.GetOrCreateForFurnitureAsync(42, 2, ct)
            );
            var nicotvId = checked((uint)nicotv.Id);

            var state = new SharedState();
            var actor = CreateVisitorSession(2);
            var peer = CreateVisitorSession(3);
            state.RegisterClient(ServerType.Area, actor);
            state.RegisterClient(ServerType.Area, peer);

            var playHandler = new AreaNicotvPlayHandler(repository, state);
            await ((IPacketHandler)playHandler).HandleAsync(
                BuildUIntPayload(nicotvId, (uint)NicotvPlaybackState.Paused),
                actor,
                ct
            );

            Assert.Equal(PacketType.NicotvPlayResponse, Assert.Single(actor.Sent).Type);
            Assert.Equal(PacketType.NotifyNicotvPlay, Assert.Single(peer.Sent).Type);
            Assert.Equal(NicotvPlaybackState.Paused, nicotv.PlaybackState);

            actor.Sent.Clear();
            peer.Sent.Clear();
            var movieHandler = new AreaNicotvSetMovieHandler(repository, state);
            await ((IPacketHandler)movieHandler).HandleAsync(
                BuildSetMoviePayload(nicotvId, "sm9"),
                actor,
                ct
            );

            Assert.Equal(PacketType.NicotvSetMovieResponse, Assert.Single(actor.Sent).Type);
            Assert.Equal(PacketType.NotifyNicotvSetMovie, Assert.Single(peer.Sent).Type);
            Assert.Equal("sm9", nicotv.MovieId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetPlayheadTime_UsesZeroWhenNoPeerIsPresent()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await SeedNicotvFurnitureAsync(options, ct);

            await using var db = new MainContext(options);
            var repository = new NicotvRepository(db);
            var nicotv = Assert.IsType<Nicotv>(
                await repository.GetOrCreateForFurnitureAsync(42, 2, ct)
            );
            var requester = CreateVisitorSession(2);
            var handler = new AreaNicotvGetPlayheadTimeHandler(repository, new SharedState());

            await ((IPacketHandler)handler).HandleAsync(
                BuildUIntPayload(checked((uint)nicotv.Id)),
                requester,
                ct
            );

            var response = Assert.Single(requester.Sent);
            Assert.Equal(PacketType.NicotvGetPlayheadTimeResponse, response.Type);
            Assert.Equal(BuildUIntPayload(checked((uint)nicotv.Id), 0), response.Payload);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static CapturingPlayerSession CreateVisitorSession(int userId = 99) =>
        new()
        {
            UserId = userId,
            CharacterId = checked((uint)userId),
            MapId = 20_000_000,
            MyRoomId = 42,
            ChannelId = 1,
        };

    private static byte[] BuildGetInfoPayload(uint furnitureId)
    {
        var writer = new PacketWriter();
        writer.Write(furnitureId);
        return writer.ToBytes();
    }

    private static byte[] BuildOpenPayload(uint furnitureId, NicotvData data)
    {
        var writer = new PacketWriter();
        writer.Write(furnitureId);
        writer.Write(data.ToBytes());
        return writer.ToBytes();
    }

    private static byte[] BuildUIntPayload(params uint[] values)
    {
        var writer = new PacketWriter();
        foreach (var value in values)
            writer.Write(value);
        return writer.ToBytes();
    }

    private static byte[] BuildSetMoviePayload(uint nicotvId, string movieId)
    {
        var writer = new PacketWriter();
        writer.Write(nicotvId);
        writer.Write(System.Text.Encoding.ASCII.GetBytes(movieId));
        writer.Write((byte)0);
        return writer.ToBytes();
    }

    private static async Task SeedNicotvFurnitureAsync(
        DbContextOptions<MainContext> options,
        CancellationToken ct
    )
    {
        await TestDb.SeedCharacterAsync(options, 42, ct);
        await using var db = new MainContext(options);
        db.Items.Add(
            new Item
            {
                Id = 11_000_590,
                Name = "ブラウン管TV（１４インチ）",
                IconId = 11_000_590,
            }
        );
        db.Furniture.Add(
            new Furniture { ItemId = 11_000_590, PlacementFlags = FurniturePlacementFlags.Floor }
        );
        db.MyRoomFurniture.Add(
            new MyRoomFurniture
            {
                RoomId = 42,
                FurnitureId = 2,
                ItemId = 11_000_590,
            }
        );
        await db.SaveChangesAsync(ct);
    }
}
