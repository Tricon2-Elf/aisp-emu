using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace aisp.Common.Tests;

public class AreaMyRoomEditingHandlerTests
{
    [Fact]
    public async Task SetUpdateRemoveFurniture_PersistsAndNotifiesOtherPlayersInTheSameRoom()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await SeedFurnitureInventoryAsync(options, 42, 7001, 1);

            await using var db = new MainContext(options);
            var repository = new MyRoomRepository(db);
            var session = CreateSession();
            var roomPeer = CreateSession();
            roomPeer.CharacterId = 43;
            var otherRoomPeer = CreateSession();
            otherRoomPeer.CharacterId = 44;
            otherRoomPeer.MyRoomId = 44;
            var state = new SharedState();
            state.RegisterClient(ServerType.Area, session);
            state.RegisterClient(ServerType.Area, roomPeer);
            state.RegisterClient(ServerType.Area, otherRoomPeer);

            var setHandler = new AreaMyRoomSetFurnitureHandler(
                repository,
                state,
                NullLogger<AreaMyRoomSetFurnitureHandler>.Instance
            );
            await setHandler.HandleAsync(
                BuildPlacementPayload(42, 7001, 0f, 0f, 0f, 0, 0),
                session,
                TestContext.Current.CancellationToken
            );

            var previewResponse = Assert.Single(session.Sent);
            Assert.Equal(PacketType.MyRoomSetFurnitureResponse, previewResponse.Type);
            Assert.Equal(0u, new PacketReader(previewResponse.Payload).ReadUInt());
            Assert.Empty(roomPeer.Sent);
            Assert.Empty(otherRoomPeer.Sent);
            Assert.Empty(
                await repository.GetFurnitureAsync(42, TestContext.Current.CancellationToken)
            );

            session.Sent.Clear();
            await setHandler.HandleAsync(
                BuildPlacementPayload(42, 7001, 1f, 2f, 3f, 4, 5),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                setResponse =>
                {
                    Assert.Equal(PacketType.MyRoomSetFurnitureResponse, setResponse.Type);
                    Assert.Equal(0u, new PacketReader(setResponse.Payload).ReadUInt());
                },
                setNotification =>
                {
                    Assert.Equal(PacketType.NotifyMyRoomSetFurniture, setNotification.Type);
                    AssertFurniture(
                        setNotification.Payload,
                        roomId: 42,
                        furnitureId: 1,
                        serialId: 7001,
                        x: 1f,
                        y: 2f,
                        z: 3f,
                        directionX: 4,
                        directionY: 5
                    );
                },
                inventoryUpdate =>
                {
                    AssertFurnitureUnavailable(inventoryUpdate, itemId: 7001);
                }
            );
            var setNotification = Assert.Single(roomPeer.Sent);
            Assert.Equal(PacketType.NotifyMyRoomSetFurniture, setNotification.Type);
            AssertFurniture(
                setNotification.Payload,
                roomId: 42,
                furnitureId: 1,
                serialId: 7001,
                x: 1f,
                y: 2f,
                z: 3f,
                directionX: 4,
                directionY: 5
            );
            Assert.Empty(otherRoomPeer.Sent);

            var stored = Assert.Single(
                await repository.GetFurnitureAsync(42, TestContext.Current.CancellationToken)
            );
            Assert.Equal(1u, stored.FurnitureId);
            Assert.Equal(7001, stored.ItemId);

            session.Sent.Clear();
            roomPeer.Sent.Clear();
            var updateHandler = new AreaMyRoomUpdateFurnitureHandler(repository, state);
            await updateHandler.HandleAsync(
                BuildPlacementPayload(42, 1, 10f, 20f, 30f, 40, 50),
                session,
                TestContext.Current.CancellationToken
            );

            var updateResponse = Assert.Single(session.Sent);
            Assert.Equal(PacketType.MyRoomUpdateFurnitureResponse, updateResponse.Type);
            Assert.Equal(0u, new PacketReader(updateResponse.Payload).ReadUInt());
            var updateNotification = Assert.Single(roomPeer.Sent);
            Assert.Equal(PacketType.NotifyMyRoomUpdateFurniture, updateNotification.Type);
            var updateReader = new PacketReader(updateNotification.Payload);
            Assert.Equal(42u, updateReader.ReadUInt());
            Assert.Equal(1u, updateReader.ReadUInt());
            Assert.Equal(10f, updateReader.ReadFloat());
            Assert.Equal(20f, updateReader.ReadFloat());
            Assert.Equal(30f, updateReader.ReadFloat());
            Assert.Equal((byte)40, updateReader.ReadByte());
            Assert.Equal((byte)50, updateReader.ReadByte());
            Assert.Empty(otherRoomPeer.Sent);

            session.Sent.Clear();
            roomPeer.Sent.Clear();
            var removeHandler = new AreaMyRoomRemoveFurnitureHandler(repository, state);
            await removeHandler.HandleAsync(
                BuildPairPayload(42, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                removeResponse =>
                {
                    Assert.Equal(PacketType.MyRoomRemoveFurnitureResponse, removeResponse.Type);
                    Assert.Equal(0u, new PacketReader(removeResponse.Payload).ReadUInt());
                },
                itemCreate => AssertInventoryItemCreated(itemCreate, itemId: 7001, quantity: 1),
                inventoryUpdate => AssertInventoryCount(inventoryUpdate, itemId: 7001, quantity: 1)
            );
            var removeNotification = Assert.Single(roomPeer.Sent);
            Assert.Equal(PacketType.NotifyMyRoomRemoveFurniture, removeNotification.Type);
            var removeReader = new PacketReader(removeNotification.Payload);
            Assert.Equal(42u, removeReader.ReadUInt());
            Assert.Equal(1u, removeReader.ReadUInt());
            Assert.Empty(otherRoomPeer.Sent);
            Assert.Empty(
                await repository.GetFurnitureAsync(42, TestContext.Current.CancellationToken)
            );
            Assert.Equal(
                1,
                await db
                    .CharacterInventories.Where(x => x.CharacterId == 42 && x.ItemId == 7001)
                    .Select(x => x.Quantity)
                    .SingleAsync(TestContext.Current.CancellationToken)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task NameAndSecurity_ArePersistedForFutureMyRoomTransfers()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);

            await using var db = new MainContext(options);
            var repository = new MyRoomRepository(db);
            var session = CreateSession();
            session.Character = await db.Characters.SingleAsync(
                character => character.Id == 42,
                TestContext.Current.CancellationToken
            );

            var nameHandler = new AreaMyRoomUpdateNameHandler(repository, WordFilter.FromTerms([]));
            var state = new SharedState();
            state.RegisterClient(ServerType.Area, session);
            var securityHandler = CreateSecurityHandler(options, repository, state);
            await ((IPacketHandler)nameHandler).HandleAsync(
                BuildNamePayload(42, "テスト部屋"),
                session,
                TestContext.Current.CancellationToken
            );
            await ((IPacketHandler)securityHandler).HandleAsync(
                BuildPairPayload(42, 2),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Contains(
                session.Sent,
                packet =>
                    packet.Type == PacketType.MyRoomUpdateSecurityResponse
                    && new PacketReader(packet.Payload).ReadUInt() == 0u
            );
            Assert.Contains(
                session.Sent,
                packet => packet.Type == PacketType.NotifyMyHouseChangeSecurity
            );

            db.ChangeTracker.Clear();
            var stored = await db.Rooms.SingleAsync(
                room => room.Id == 42,
                TestContext.Current.CancellationToken
            );
            Assert.Equal("テスト部屋", stored.Name);
            Assert.Equal(MyRoomSecurity.CircleMembersOnly, stored.Security);

            session.Sent.Clear();
            await ((IPacketHandler)securityHandler).HandleAsync(
                BuildPairPayload(42, 5),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(
                1u,
                new PacketReader(
                    Assert
                        .Single(
                            session.Sent,
                            packet => packet.Type == PacketType.MyRoomUpdateSecurityResponse
                        )
                        .Payload
                ).ReadUInt()
            );

            db.ChangeTracker.Clear();
            stored = await db.Rooms.SingleAsync(
                room => room.Id == 42,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(MyRoomSecurity.CircleMembersOnly, stored.Security);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UpdateName_RejectsBlockedRoomName()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);

            await using var db = new MainContext(options);
            var repository = new MyRoomRepository(db);
            var session = CreateSession();
            var handler = new AreaMyRoomUpdateNameHandler(
                repository,
                WordFilter.FromTerms(["faggot"])
            );

            await ((IPacketHandler)handler).HandleAsync(
                BuildNamePayload(42, "Faggot"),
                session,
                TestContext.Current.CancellationToken
            );

            var response = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.MyRoomUpdateNameResponse
            );
            Assert.Equal(1u, new PacketReader(response.Payload).ReadUInt());

            db.ChangeTracker.Clear();
            var stored = await db.Rooms.SingleAsync(
                room => room.Id == 42,
                TestContext.Current.CancellationToken
            );
            Assert.Equal("My Room", stored.Name);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task FurnitureMutation_RejectsAnotherPlayersRoom()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await using var db = new MainContext(options);
            var handler = new AreaMyRoomSetFurnitureHandler(
                new MyRoomRepository(db),
                new SharedState(),
                NullLogger<AreaMyRoomSetFurnitureHandler>.Instance
            );
            var session = CreateSession();

            await handler.HandleAsync(
                BuildPlacementPayload(99, 7001, 1f, 2f, 3f, 4, 5),
                session,
                TestContext.Current.CancellationToken
            );

            var response = Assert.Single(session.Sent);
            Assert.Equal(PacketType.MyRoomSetFurnitureResponse, response.Type);
            Assert.Equal(1u, new PacketReader(response.Payload).ReadUInt());
            Assert.Empty(
                await db.MyRoomFurniture.ToListAsync(TestContext.Current.CancellationToken)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SetFurniture_RequiresAnOwnedCatalogItemAndReservesEachPlacedCopy()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await SeedFurnitureCatalogItemAsync(options, 7001);

            await using var db = new MainContext(options);
            var repository = new MyRoomRepository(db);
            var state = new SharedState();
            var handler = new AreaMyRoomSetFurnitureHandler(
                repository,
                state,
                NullLogger<AreaMyRoomSetFurnitureHandler>.Instance
            );
            var session = CreateSession();

            await handler.HandleAsync(
                BuildPlacementPayload(42, 7001, 0, 0, 0, 0, 0),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(1u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());

            db.CharacterInventories.Add(
                new CharacterInventory
                {
                    CharacterId = 42,
                    ItemId = 7001,
                    Quantity = 1,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            session.Sent.Clear();
            await handler.HandleAsync(
                BuildPlacementPayload(42, 7001, 1, 2, 3, 4, 5),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(1u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());
            Assert.Empty(
                await repository.GetFurnitureAsync(42, TestContext.Current.CancellationToken)
            );

            session.Sent.Clear();
            await handler.HandleAsync(
                BuildPlacementPayload(42, 7001, 0, 0, 0, 0, 0),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(0u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());
            Assert.Empty(
                await repository.GetFurnitureAsync(42, TestContext.Current.CancellationToken)
            );

            session.Sent.Clear();
            await handler.HandleAsync(
                BuildPlacementPayload(42, 7001, 1, 2, 3, 4, 5),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());

            session.Sent.Clear();
            await handler.HandleAsync(
                BuildPlacementPayload(42, 7001, 0, 0, 0, 0, 0),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(1u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());
            Assert.Single(
                await repository.GetFurnitureAsync(42, TestContext.Current.CancellationToken)
            );
            Assert.Equal(
                1,
                await db
                    .CharacterInventories.Where(x => x.CharacterId == 42 && x.ItemId == 7001)
                    .Select(x => x.Quantity)
                    .SingleAsync(TestContext.Current.CancellationToken)
            );

            session.Sent.Clear();
            await new AreaMyRoomRemoveFurnitureHandler(repository, state).HandleAsync(
                BuildPairPayload(42, 1),
                session,
                TestContext.Current.CancellationToken
            );
            session.Sent.Clear();
            await handler.HandleAsync(
                BuildPlacementPayload(42, 7001, 0, 0, 0, 0, 0),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(0u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());
            session.Sent.Clear();
            await handler.HandleAsync(
                BuildPlacementPayload(42, 7001, 6, 7, 8, 9, 10),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());
            Assert.Single(
                await repository.GetFurnitureAsync(42, TestContext.Current.CancellationToken)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task StartAndEndFurniture_ClearPendingPreviewReservation()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var session = CreateSession();
            session.PendingMyRoomFurnitureItemId = 7001;

            var payload = BuildRoomPayload(session.MyRoomId);
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await using var db = new MainContext(options);
            var repository = new MyRoomRepository(db);

            await new AreaMyRoomStartFurnitureHandler(
                repository,
                NullLogger<AreaMyRoomStartFurnitureHandler>.Instance
            ).HandleAsync(payload, session, TestContext.Current.CancellationToken);
            Assert.Null(session.PendingMyRoomFurnitureItemId);

            session.PendingMyRoomFurnitureItemId = 7001;
            await new AreaMyRoomEndFurnitureHandler(
                repository,
                NullLogger<AreaMyRoomEndFurnitureHandler>.Instance
            ).HandleAsync(payload, session, TestContext.Current.CancellationToken);
            Assert.Null(session.PendingMyRoomFurnitureItemId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetFurniture_ResynchronizesAvailableInventoryBeforeEditingStarts()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await SeedFurnitureInventoryAsync(options, 42, 7001, 2);

            await using var db = new MainContext(options);
            db.MyRoomFurniture.Add(
                new MyRoomFurniture
                {
                    RoomId = 42,
                    FurnitureId = 1,
                    ItemId = 7001,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var session = CreateSession();
            var handler = new AreaMyRoomGetFurnitureHandler(
                new RoboRepository(db),
                new MyRoomRepository(db),
                new SharedState()
            );
            var writer = new PacketWriter();
            writer.Write(session.MapId);
            writer.Write(checked((uint)session.ChannelId));

            await handler.HandleAsync(
                writer.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                furniture => Assert.Equal(PacketType.MyRoomNotifyFurniture, furniture.Type),
                itemCreate => AssertInventoryItemCreated(itemCreate, itemId: 7001, quantity: 1),
                inventoryUpdate => AssertInventoryCount(inventoryUpdate, itemId: 7001, quantity: 1),
                response => Assert.Equal(PacketType.MyRoomGetFurnitureResponse, response.Type)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SetFurniture_RejectsOwnedItemsThatAreNotFurniture()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await using (var seedDb = new MainContext(options))
            {
                seedDb.Items.Add(new Item { Id = 7001, Name = "Not furniture" });
                seedDb.CharacterInventories.Add(
                    new CharacterInventory
                    {
                        CharacterId = 42,
                        ItemId = 7001,
                        Quantity = 1,
                    }
                );
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var db = new MainContext(options);
            var session = CreateSession();
            await new AreaMyRoomSetFurnitureHandler(
                new MyRoomRepository(db),
                new SharedState(),
                NullLogger<AreaMyRoomSetFurnitureHandler>.Instance
            ).HandleAsync(
                BuildPlacementPayload(42, 7001, 0, 0, 0, 0, 0),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(1u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());
            Assert.Empty(
                await db.MyRoomFurniture.ToListAsync(TestContext.Current.CancellationToken)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task RemoveInventory_RejectsRemovingAPlacedFurnitureCopy()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await SeedFurnitureInventoryAsync(options, 42, 7001, 1);

            await using var db = new MainContext(options);
            db.MyRoomFurniture.Add(
                new MyRoomFurniture
                {
                    RoomId = 42,
                    FurnitureId = 1,
                    ItemId = 7001,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            var repository = new CharacterRepository(db, NullLogger<CharacterRepository>.Instance);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.RemoveInventoryAsync(42, 7001, 1, TestContext.Current.CancellationToken)
            );

            Assert.Contains("currently placed", exception.Message);
            Assert.Equal(
                1,
                await db
                    .CharacterInventories.Where(x => x.CharacterId == 42 && x.ItemId == 7001)
                    .Select(x => x.Quantity)
                    .SingleAsync(TestContext.Current.CancellationToken)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task FurnitureBaseList_ReturnsPersistedCatalog()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await SeedFurnitureCatalogItemAsync(options, 7001, FurniturePlacementFlags.Wall);

            await using var db = new MainContext(options);
            var response = await new AreaFurnitureGetBaseListHandler(
                new MyRoomRepository(db)
            ).HandleAsync(
                new FurnitureGetBaseListRequest(),
                CreateSession(),
                TestContext.Current.CancellationToken
            );
            var entry = Assert.Single(
                Assert.IsType<FurnitureGetBaseListResponse>(response).Entries
            );

            Assert.Equal(7001u, entry.ItemId);
            Assert.Equal(0u, entry.Type);
            Assert.Equal(FurniturePlacementFlags.Wall, entry.PlacementFlags);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task FurnitureSeed_LoadsTheCompleteClientCatalogAndIsIdempotent()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var seedPath = Path.Combine(AppContext.BaseDirectory, "seedData", "furniture.json");

            await MyRoomRepository.EnsureFurnitureCatalogPresentAsync(
                db,
                seedPath,
                TestContext.Current.CancellationToken
            );
            await MyRoomRepository.EnsureFurnitureCatalogPresentAsync(
                db,
                seedPath,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(249, await db.Furniture.CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(
                "カントリーなベッド（ピンク）",
                await db
                    .Items.Where(x => x.Id == 11_000_000)
                    .Select(x => x.Name)
                    .SingleAsync(TestContext.Current.CancellationToken)
            );
            Assert.Equal(
                FurniturePlacementFlags.Wall,
                await db
                    .Furniture.Where(x => x.ItemId == 11_001_020)
                    .Select(x => x.PlacementFlags)
                    .SingleAsync(TestContext.Current.CancellationToken)
            );
            Assert.Equal(
                FurniturePlacementFlags.Ceiling,
                await db
                    .Furniture.Where(x => x.ItemId == 11_001_140)
                    .Select(x => x.PlacementFlags)
                    .SingleAsync(TestContext.Current.CancellationToken)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task FurnitureInventory_IsSharedAcrossAllRoomsOwnedByTheCharacter()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await SeedFurnitureInventoryAsync(options, 42, 7001, 1);

            await using var db = new MainContext(options);
            db.Rooms.Add(
                new Room
                {
                    Id = 43,
                    OwnerCharacterId = 42,
                    Name = "Second Room",
                    Stage = MyRoomStage.EightTatami,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var repository = new MyRoomRepository(db);
            var placed = await repository.TryAddFurnitureAsync(
                42,
                new MyRoomFurniture { RoomId = 42, ItemId = 7001 },
                700,
                TestContext.Current.CancellationToken
            );

            Assert.NotNull(placed);
            Assert.False(
                await repository.CanPlaceFurnitureAsync(
                    42,
                    43,
                    7001,
                    700,
                    TestContext.Current.CancellationToken
                )
            );
            Assert.Equal(
                0,
                (
                    await repository.GetAvailableFurnitureInventoryAsync(
                        42,
                        TestContext.Current.CancellationToken
                    )
                )[7001]
            );

            await repository.RemoveFurnitureAsync(
                42,
                placed.FurnitureId,
                TestContext.Current.CancellationToken
            );

            Assert.True(
                await repository.CanPlaceFurnitureAsync(
                    42,
                    43,
                    7001,
                    700,
                    TestContext.Current.CancellationToken
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static AreaMyRoomUpdateSecurityHandler CreateSecurityHandler(
        DbContextOptions<MainContext> options,
        MyRoomRepository repository,
        SharedState state
    )
    {
        var transition = new DirectMapLinkTransitionService(
            new MapRepository(new MainContext(options)),
            new CharacterRepository(
                new MainContext(options),
                NullLogger<CharacterRepository>.Instance
            ),
            new MyRoomRepository(new MainContext(options)),
            new CircleRepository(new MainContext(options)),
            new MapLinkRepository(new MainContext(options)),
            new ChannelRepository(new MainContext(options)),
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
        );
        return new AreaMyRoomUpdateSecurityHandler(
            repository,
            state,
            transition,
            NullLogger<AreaMyRoomUpdateSecurityHandler>.Instance
        );
    }

    private static CapturingPlayerSession CreateSession() =>
        new()
        {
            CharacterId = 42,
            MapId = MyRoomInfo.BaseMapId,
            MyRoomId = 42,
            ChannelId = 1,
        };

    private static async Task SeedFurnitureInventoryAsync(
        DbContextOptions<MainContext> options,
        int characterId,
        int itemId,
        int quantity
    )
    {
        await SeedFurnitureCatalogItemAsync(options, itemId);
        await using var db = new MainContext(options);
        db.CharacterInventories.Add(
            new CharacterInventory
            {
                CharacterId = characterId,
                ItemId = itemId,
                Quantity = quantity,
            }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task SeedFurnitureCatalogItemAsync(
        DbContextOptions<MainContext> options,
        int itemId,
        FurniturePlacementFlags placementFlags = FurniturePlacementFlags.Floor
    )
    {
        await using var db = new MainContext(options);
        db.Items.Add(new Item { Id = itemId, Name = $"Furniture {itemId}" });
        db.Furniture.Add(new Furniture { ItemId = itemId, PlacementFlags = placementFlags });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static byte[] BuildPlacementPayload(
        uint roomId,
        uint secondId,
        float x,
        float y,
        float z,
        byte directionX,
        byte directionY
    )
    {
        var writer = new PacketWriter();
        writer.Write(roomId);
        writer.Write(secondId);
        writer.Write(x);
        writer.Write(y);
        writer.Write(z);
        writer.Write(directionX);
        writer.Write(directionY);
        return writer.ToBytes();
    }

    private static byte[] BuildPairPayload(uint first, uint second)
    {
        var writer = new PacketWriter();
        writer.Write(first);
        writer.Write(second);
        return writer.ToBytes();
    }

    private static byte[] BuildRoomPayload(uint roomId)
    {
        var writer = new PacketWriter();
        writer.Write(roomId);
        return writer.ToBytes();
    }

    private static byte[] BuildNamePayload(uint roomId, string name)
    {
        var writer = new PacketWriter();
        writer.Write(roomId);
        writer.Write(name);
        return writer.ToBytes();
    }

    private static void AssertFurniture(
        byte[] payload,
        uint roomId,
        uint furnitureId,
        uint serialId,
        float x,
        float y,
        float z,
        byte directionX,
        byte directionY
    )
    {
        Assert.Equal(MyRoomFurnitureData.WireSize, payload.Length);
        var reader = new PacketReader(payload);
        Assert.Equal(roomId, reader.ReadUInt());
        Assert.Equal(furnitureId, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(serialId, reader.ReadUInt());
        Assert.Equal(x, reader.ReadFloat());
        Assert.Equal(y, reader.ReadFloat());
        Assert.Equal(z, reader.ReadFloat());
        Assert.Equal(directionX, reader.ReadByte());
        Assert.Equal(directionY, reader.ReadByte());
        Assert.Equal(1u, reader.ReadUInt());
    }

    private static void AssertInventoryCount(
        (PacketType Type, byte[] Payload) packet,
        uint itemId,
        uint quantity
    )
    {
        Assert.Equal(PacketType.ItemUpdateListNotify, packet.Type);
        var reader = new PacketReader(packet.Payload);
        Assert.Equal(CharacterItemSync.PrimaryItemTablePlace, reader.ReadUInt());
        Assert.Equal(itemId, reader.ReadUInt());
        Assert.Equal(quantity, reader.ReadUInt());
    }

    private static void AssertFurnitureUnavailable(
        (PacketType Type, byte[] Payload) packet,
        uint itemId
    )
    {
        Assert.Equal(PacketType.ItemDeleteNotify, packet.Type);
        var reader = new PacketReader(packet.Payload);
        Assert.Equal(CharacterItemSync.PrimaryItemTablePlace, reader.ReadUInt());
        Assert.Equal(itemId, reader.ReadUInt());
    }

    private static void AssertInventoryItemCreated(
        (PacketType Type, byte[] Payload) packet,
        uint itemId,
        ushort quantity
    )
    {
        Assert.Equal(PacketType.ItemCreateNotify, packet.Type);
        var reader = new PacketReader(packet.Payload);
        Assert.Equal(CharacterItemSync.PrimaryItemTablePlace, reader.ReadUInt());
        Assert.Equal(itemId, reader.ReadUInt());
        Assert.Equal(quantity, reader.ReadUShort());
        Assert.Equal(itemId, reader.ReadUInt());
        Assert.Equal(0ul, reader.ReadULong());
    }
}
