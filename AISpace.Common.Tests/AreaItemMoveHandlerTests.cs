using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

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
            var character = new Character
            {
                UserId = user.Id,
                Name = "Mover2",
            };
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
