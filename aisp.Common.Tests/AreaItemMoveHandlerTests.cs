using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AreaItemMoveHandlerTests
{
    [Fact]
    public async Task HandleAsync_InventoryToStorage_MovesStackAndSyncsBothPlaces()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = new User { Username = "mover" };
            user.SetPassword("password");
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var item = new Item { Id = 10_504_106, Name = "Test Top" };
            db.Items.Add(item);
            var character = new Character
            {
                UserId = user.Id,
                Name = "Mover",
                Inventory =
                {
                    new CharacterInventory { ItemId = item.Id, Quantity = 3 },
                },
            };
            db.Characters.Add(character);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)character.Id,
                User = new User { Id = user.Id },
            };
            var handler = new AreaItemMoveHandler(
                new UserRepository(db),
                NullLogger<AreaItemMoveHandler>.Instance
            );

            var payload = new byte[18];
            BitConverter.TryWriteBytes(payload.AsSpan(0), 0u);
            BitConverter.TryWriteBytes(payload.AsSpan(4), (uint)item.Id);
            BitConverter.TryWriteBytes(payload.AsSpan(8), (ushort)1);
            BitConverter.TryWriteBytes(payload.AsSpan(10), 1u);
            BitConverter.TryWriteBytes(payload.AsSpan(14), 0u);

            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            Assert.Contains(
                session.Sent,
                p =>
                    p.Type == PacketType.ItemMoveResponse
                    && new PacketReader(p.Payload).ReadUInt() == 0
            );

            var inventory = await db.CharacterInventories.FindAsync(
                [character.Id, item.Id],
                TestContext.Current.CancellationToken
            );
            Assert.Equal(2, inventory!.Quantity);

            var storage = Assert.Single(db.UserStorageItems);
            Assert.Equal(user.Id, storage.UserId);
            Assert.Equal(item.Id, storage.ItemId);
            Assert.Equal(1, storage.Quantity);

            Assert.Contains(
                session.Sent,
                p => p.Type == PacketType.ItemCreateNotify && p.Payload[0] == 1
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_InventoryToStorage_RejectsMovingPlacedFurnitureCopies()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await using var db = new MainContext(options);
            db.Items.Add(new Item { Id = 7001, Name = "Placed Chair" });
            db.Furniture.Add(new Furniture { ItemId = 7001 });
            db.CharacterInventories.Add(
                new CharacterInventory
                {
                    CharacterId = 42,
                    ItemId = 7001,
                    Quantity = 2,
                }
            );
            db.MyRoomFurniture.Add(
                new MyRoomFurniture
                {
                    RoomId = 42,
                    FurnitureId = 1,
                    ItemId = 7001,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var session = new CapturingPlayerSession
            {
                CharacterId = 42,
                User = new User { Id = 42 },
            };
            var handler = new AreaItemMoveHandler(
                new UserRepository(db),
                NullLogger<AreaItemMoveHandler>.Instance
            );

            // Attempt to warehouse the entire stack (including the placed copy).
            var payload = new byte[18];
            BitConverter.TryWriteBytes(payload.AsSpan(0), 0u);
            BitConverter.TryWriteBytes(payload.AsSpan(4), 7001u);
            BitConverter.TryWriteBytes(payload.AsSpan(8), (ushort)2);
            BitConverter.TryWriteBytes(payload.AsSpan(10), 1u);
            BitConverter.TryWriteBytes(payload.AsSpan(14), 0u);

            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            Assert.Contains(
                session.Sent,
                p =>
                    p.Type == PacketType.ItemMoveResponse
                    && new PacketReader(p.Payload).ReadUInt() == 1
            );
            Assert.Equal(
                2,
                await db
                    .CharacterInventories.Where(x => x.CharacterId == 42 && x.ItemId == 7001)
                    .Select(x => x.Quantity)
                    .SingleAsync(TestContext.Current.CancellationToken)
            );
            Assert.Empty(db.UserStorageItems);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_InventoryToStorage_AllowsMovingUnplacedFurnitureCopies()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await using var db = new MainContext(options);
            db.Items.Add(new Item { Id = 7001, Name = "Placed Chair" });
            db.Furniture.Add(new Furniture { ItemId = 7001 });
            db.CharacterInventories.Add(
                new CharacterInventory
                {
                    CharacterId = 42,
                    ItemId = 7001,
                    Quantity = 2,
                }
            );
            db.MyRoomFurniture.Add(
                new MyRoomFurniture
                {
                    RoomId = 42,
                    FurnitureId = 1,
                    ItemId = 7001,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var session = new CapturingPlayerSession
            {
                CharacterId = 42,
                User = new User { Id = 42 },
            };
            var handler = new AreaItemMoveHandler(
                new UserRepository(db),
                NullLogger<AreaItemMoveHandler>.Instance
            );

            // Warehouse only the spare (unplaced) copy.
            var payload = new byte[18];
            BitConverter.TryWriteBytes(payload.AsSpan(0), 0u);
            BitConverter.TryWriteBytes(payload.AsSpan(4), 7001u);
            BitConverter.TryWriteBytes(payload.AsSpan(8), (ushort)1);
            BitConverter.TryWriteBytes(payload.AsSpan(10), 1u);
            BitConverter.TryWriteBytes(payload.AsSpan(14), 0u);

            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            Assert.Contains(
                session.Sent,
                p =>
                    p.Type == PacketType.ItemMoveResponse
                    && new PacketReader(p.Payload).ReadUInt() == 0
            );
            Assert.Equal(
                1,
                await db
                    .CharacterInventories.Where(x => x.CharacterId == 42 && x.ItemId == 7001)
                    .Select(x => x.Quantity)
                    .SingleAsync(TestContext.Current.CancellationToken)
            );
            var storage = Assert.Single(db.UserStorageItems);
            Assert.Equal(7001, storage.ItemId);
            Assert.Equal(1, storage.Quantity);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_StorageToInventory_RestoresStack()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = new User { Username = "mover2" };
            user.SetPassword("password");
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var item = new Item { Id = 10_504_107, Name = "Test Bottom" };
            db.Items.Add(item);
            var character = new Character { UserId = user.Id, Name = "Mover2" };
            db.Characters.Add(character);
            db.UserStorageItems.Add(
                new UserStorageItem
                {
                    UserId = user.Id,
                    ItemId = item.Id,
                    Quantity = 2,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)character.Id,
                User = new User { Id = user.Id },
            };
            var handler = new AreaItemMoveHandler(
                new UserRepository(db),
                NullLogger<AreaItemMoveHandler>.Instance
            );

            var payload = new byte[18];
            BitConverter.TryWriteBytes(payload.AsSpan(0), 1u);
            BitConverter.TryWriteBytes(payload.AsSpan(4), (uint)item.Id);
            BitConverter.TryWriteBytes(payload.AsSpan(8), (ushort)2);
            BitConverter.TryWriteBytes(payload.AsSpan(10), 0u);
            BitConverter.TryWriteBytes(payload.AsSpan(14), 0u);

            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            Assert.Null(
                await db.UserStorageItems.FindAsync(
                    [user.Id, item.Id],
                    TestContext.Current.CancellationToken
                )
            );
            var inventory = Assert.Single(db.CharacterInventories);
            Assert.Equal(2, inventory.Quantity);
            Assert.Contains(
                session.Sent,
                p => p.Type == PacketType.ItemDeleteNotify && p.Payload[0] == 1
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
