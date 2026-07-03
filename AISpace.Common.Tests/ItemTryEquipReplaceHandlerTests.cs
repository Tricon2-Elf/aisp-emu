using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class ItemTryEquipReplaceHandlerTests
{
    [Fact]
    public async Task HandleAsync_RefreshesSessionCharacterAfterSuccessfulReplace()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int characterId = 12001;
            const int oldTopId = 10100060;
            const int newTopId = 10100220;

            await using (var db = new MainContext(options))
            {
                var user = new User { Id = 1, Username = "replace-refresh-user" };
                user.SetPassword("pw");
                db.Users.Add(user);

                db.Characters.Add(
                    new Character
                    {
                        Id = characterId,
                        UserId = user.Id,
                        Name = "Replace Refresh",
                        ModelId = 100,
                        Birthdate = new DateTime(2000, 1, 1),
                        BloodType = BloodType.A,
                        Gender = 1,
                        FaceType = 1,
                        Hairstyle = 1,
                        CurrentMapId = 10990100,
                    }
                );

                db.Items.AddRange(new Item { Id = oldTopId, Name = "Old Top", Socket = 8 }, new Item { Id = newTopId, Name = "New Top", Socket = 8 });
                db.CharacterEquipments.Add(new CharacterEquipment { CharacterId = characterId, SlotIndex = 1, ItemId = oldTopId });
                db.CharacterInventories.Add(new CharacterInventory { CharacterId = characterId, ItemId = newTopId, Quantity = 1 });

                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var staleCharacter = new Character
            {
                Id = characterId,
                Equipment = [new CharacterEquipment { CharacterId = characterId, SlotIndex = 1, ItemId = oldTopId }],
                Inventory = [],
            };

            var state = new SharedState();
            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)characterId,
                Character = staleCharacter,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, session);
            var repo = new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance);
            var handler = new ItemTryEquipReplaceHandler(repo, state, NullLogger<ItemTryEquipReplaceHandler>.Instance);

            var payload = BuildReplaceRequestPayload(objId: 1, equips: [new ItemEquipEntry((uint)newTopId, 8)]);
            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            Assert.NotNull(session.Character);
            Assert.NotSame(staleCharacter, session.Character);
            Assert.Contains(session.Character!.Equipment, e => e.ItemId == newTopId);
            Assert.DoesNotContain(session.Character.Equipment, e => e.ItemId == oldTopId);
            Assert.Contains(session.Character.Inventory, i => i.ItemId == oldTopId && i.Quantity == 1);

            Assert.Contains(session.Sent, packet => packet.Type == PacketType.ItemTryEquipReplaceResponse);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.ItemTryEquipReplacedNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_BroadcastsUpdatedAppearanceToAreaPeers()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int characterId = 12002;
            const int oldTopId = 10100060;
            const int newTopId = 10100220;

            await using (var db = new MainContext(options))
            {
                var user = new User { Id = 2, Username = "replace-broadcast-user" };
                user.SetPassword("pw");
                db.Users.Add(user);

                db.Characters.Add(
                    new Character
                    {
                        Id = characterId,
                        UserId = user.Id,
                        Name = "Replace Broadcast",
                        ModelId = 100,
                        Birthdate = new DateTime(2000, 1, 1),
                        BloodType = BloodType.A,
                        Gender = 1,
                        FaceType = 1,
                        Hairstyle = 1,
                        CurrentMapId = 10990100,
                    }
                );

                db.Items.AddRange(new Item { Id = oldTopId, Name = "Old Top", Socket = 8 }, new Item { Id = newTopId, Name = "New Top", Socket = 8 });
                db.CharacterEquipments.Add(new CharacterEquipment { CharacterId = characterId, SlotIndex = 1, ItemId = oldTopId });
                db.CharacterInventories.Add(new CharacterInventory { CharacterId = characterId, ItemId = newTopId, Quantity = 1 });

                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var actor = new CapturingPlayerSession
            {
                CharacterId = (uint)characterId,
                MapId = 10990100,
                ChannelId = 1,
                X = 10,
                Y = 2,
                Z = 20,
                Rotation = 90,
            };
            var peer = new CapturingPlayerSession
            {
                CharacterId = 22002,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, actor);
            state.RegisterClient(ServerType.Area, peer);

            var repo = new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance);
            var handler = new ItemTryEquipReplaceHandler(repo, state, NullLogger<ItemTryEquipReplaceHandler>.Instance);

            var payload = BuildReplaceRequestPayload(objId: (uint)characterId, equips: [new ItemEquipEntry((uint)newTopId, 8)]);
            await handler.HandleAsync(payload, actor, TestContext.Current.CancellationToken);

            Assert.Contains(peer.Sent, packet => packet.Type == PacketType.AvatarNotifyData);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static byte[] BuildReplaceRequestPayload(uint objId, IReadOnlyList<ItemEquipEntry> equips)
    {
        var writer = new PacketWriter();
        writer.Write(objId);
        writer.Write((uint)equips.Count);
        foreach (var equip in equips)
            writer.Write(equip.ToBytes());
        return writer.ToBytes();
    }
}
