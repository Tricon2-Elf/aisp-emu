using System.Buffers.Binary;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class ItemDiscardHandlersTests
{
    private const int ShirtId = 10_504_106;
    private const int HatId = 10_100_220;
    private const int ChairId = 11_000_100;

    [Fact]
    public async Task ItemDiscard_PartialStack_SendsUpdateNumThenResult()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var (session, character) = await SeedAsync(db);
            var handler = new ItemDiscardHandler(
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                NullLogger<ItemDiscardHandler>.Instance
            );

            await handler.HandleAsync(
                DiscardPayload((uint)ShirtId, 2),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(
                [PacketType.ItemUpdateNumNotify, PacketType.ItemDiscardResponse],
                session.Sent.Select(p => p.Type).ToList()
            );
            var update = session.Sent[0].Payload;
            Assert.Equal(10, update.Length);
            Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(update.AsSpan(0, 4)));
            Assert.Equal(
                (uint)ShirtId,
                BinaryPrimitives.ReadUInt32LittleEndian(update.AsSpan(4, 4))
            );
            Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(update.AsSpan(8, 2)));
            Assert.Equal(0u, new PacketReader(session.Sent[1].Payload).ReadUInt());

            var stack = await db.CharacterInventories.FindAsync(
                [character.Id, ShirtId],
                TestContext.Current.CancellationToken
            );
            Assert.Equal(1, stack!.Quantity);
            Assert.Equal(1, session.Character!.Inventory.Single(i => i.ItemId == ShirtId).Quantity);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ItemDiscard_WholeStack_SendsItemDelete()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var (session, character) = await SeedAsync(db);
            var handler = new ItemDiscardHandler(
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                NullLogger<ItemDiscardHandler>.Instance
            );

            await handler.HandleAsync(
                DiscardPayload((uint)ShirtId, 3),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(
                [PacketType.ItemDeleteNotify, PacketType.ItemDiscardResponse],
                session.Sent.Select(p => p.Type).ToList()
            );
            Assert.Equal(8, session.Sent[0].Payload.Length);
            Assert.Null(
                await db.CharacterInventories.FindAsync(
                    [character.Id, ShirtId],
                    TestContext.Current.CancellationToken
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Theory]
    [InlineData((uint)ShirtId, (ushort)4)] // more than owned
    [InlineData((uint)HatId, (ushort)1)] // last copy of an equipped item
    [InlineData(99_999_999u, (ushort)1)] // not in bag
    [InlineData((uint)ShirtId, (ushort)0)] // nothing to discard
    public async Task ItemDiscard_Refused_LeavesBagAloneAndFails(uint serialId, ushort num)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var (session, character) = await SeedAsync(db);
            var handler = new ItemDiscardHandler(
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                NullLogger<ItemDiscardHandler>.Instance
            );

            await handler.HandleAsync(
                DiscardPayload(serialId, num),
                session,
                TestContext.Current.CancellationToken
            );

            var only = Assert.Single(session.Sent);
            Assert.Equal(PacketType.ItemDiscardResponse, only.Type);
            Assert.Equal(1u, new PacketReader(only.Payload).ReadUInt());
            var shirt = await db.CharacterInventories.FindAsync(
                [character.Id, ShirtId],
                TestContext.Current.CancellationToken
            );
            Assert.Equal(3, shirt!.Quantity);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task TrashboxDiscard_MultipleStacks_UpdatesEachThenResult()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var (session, character) = await SeedAsync(db);
            var handler = new AreaTrashboxDiscardItemHandler(
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                NullLogger<AreaTrashboxDiscardItemHandler>.Instance
            );

            // Two entries for the shirt (client can list a serial twice) and the chair's only copy.
            await handler.HandleAsync(
                TrashboxPayload([(uint)ShirtId, (uint)ChairId, (uint)ShirtId], [1, 1, 1]),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(PacketType.TrashboxDiscardItemResponse, session.Sent[^1].Type);
            Assert.Equal(0u, new PacketReader(session.Sent[^1].Payload).ReadUInt());
            Assert.Contains(
                session.Sent,
                p =>
                    p.Type == PacketType.ItemUpdateNumNotify
                    && BinaryPrimitives.ReadUInt32LittleEndian(p.Payload.AsSpan(4, 4)) == ShirtId
                    && BinaryPrimitives.ReadUInt16LittleEndian(p.Payload.AsSpan(8, 2)) == 1
            );
            Assert.Contains(
                session.Sent,
                p =>
                    p.Type == PacketType.ItemDeleteNotify
                    && BinaryPrimitives.ReadUInt32LittleEndian(p.Payload.AsSpan(4, 4)) == ChairId
            );

            var shirt = await db.CharacterInventories.FindAsync(
                [character.Id, ShirtId],
                TestContext.Current.CancellationToken
            );
            Assert.Equal(1, shirt!.Quantity);
            Assert.Null(
                await db.CharacterInventories.FindAsync(
                    [character.Id, ChairId],
                    TestContext.Current.CancellationToken
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task TrashboxDiscard_TooManyStacks_IsRejectedByParser()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var (session, _) = await SeedAsync(db);
            var handler = new AreaTrashboxDiscardItemHandler(
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                NullLogger<AreaTrashboxDiscardItemHandler>.Instance
            );

            var serials = Enumerable.Repeat((uint)ShirtId, 11).ToArray();
            var nums = Enumerable.Repeat((ushort)1, 11).ToArray();
            await handler.HandleAsync(
                TrashboxPayload(serials, nums),
                session,
                TestContext.Current.CancellationToken
            );

            var only = Assert.Single(session.Sent);
            Assert.Equal(PacketType.TrashboxDiscardItemResponse, only.Type);
            Assert.Equal(1u, new PacketReader(only.Payload).ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public void TrashboxDiscardItemRequest_ParsesClientLayout()
    {
        var request = TrashboxDiscardItemRequest.FromBytes(TrashboxPayload([5u, 6u], [2, 7]));
        Assert.Equal([5u, 6u], request.SerialIds);
        Assert.Equal([(ushort)2, (ushort)7], request.Nums);
    }

    private static async Task<(CapturingPlayerSession Session, Character Character)> SeedAsync(
        MainContext db
    )
    {
        var user = new User { Username = "discarder" };
        user.SetPassword("password");
        db.Users.Add(user);
        db.Items.AddRange(
            new Item { Id = ShirtId, Name = "Shirt" },
            new Item { Id = HatId, Name = "Hat" },
            new Item { Id = ChairId, Name = "Chair" }
        );
        var character = new Character
        {
            User = user,
            Name = "Discarder",
            Inventory =
            {
                new CharacterInventory { ItemId = ShirtId, Quantity = 3 },
                new CharacterInventory { ItemId = HatId, Quantity = 1 },
                new CharacterInventory { ItemId = ChairId, Quantity = 1 },
            },
            Equipment =
            {
                new CharacterEquipment { SlotIndex = 0, ItemId = HatId },
            },
        };
        db.Characters.Add(character);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var session = new CapturingPlayerSession
        {
            CharacterId = (uint)character.Id,
            User = new User { Id = user.Id },
        };
        return (session, character);
    }

    private static byte[] DiscardPayload(uint serialId, ushort num)
    {
        var writer = new PacketWriter();
        writer.Write(serialId);
        writer.Write(num);
        return writer.ToBytes();
    }

    private static byte[] TrashboxPayload(uint[] serials, ushort[] nums)
    {
        var writer = new PacketWriter();
        writer.Write((uint)serials.Length);
        foreach (var s in serials)
            writer.Write(s);
        writer.Write((uint)nums.Length);
        foreach (var n in nums)
            writer.Write(n);
        return writer.ToBytes();
    }
}
