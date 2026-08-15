using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace aisp.Common.Tests;

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

                db.Items.AddRange(
                    new Item
                    {
                        Id = oldTopId,
                        Name = "Old Top",
                        Socket = 8,
                    },
                    new Item
                    {
                        Id = newTopId,
                        Name = "New Top",
                        Socket = 8,
                    }
                );
                db.CharacterEquipments.Add(
                    new CharacterEquipment
                    {
                        CharacterId = characterId,
                        SlotIndex = 1,
                        ItemId = oldTopId,
                    }
                );
                db.CharacterInventories.Add(
                    new CharacterInventory
                    {
                        CharacterId = characterId,
                        ItemId = newTopId,
                        Quantity = 1,
                    }
                );

                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var staleCharacter = new Character
            {
                Id = characterId,
                Equipment =
                [
                    new CharacterEquipment
                    {
                        CharacterId = characterId,
                        SlotIndex = 1,
                        ItemId = oldTopId,
                    },
                ],
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
            var repo = new CharacterRepository(
                new MainContext(options),
                NullLogger<CharacterRepository>.Instance
            );
            var roboRepository = new RoboRepository(new MainContext(options));
            var handler = new ItemTryEquipReplaceHandler(
                repo,
                roboRepository,
                state,
                NullLogger<ItemTryEquipReplaceHandler>.Instance
            );

            var payload = BuildReplaceRequestPayload(
                objId: characterId,
                equips: [new ItemEquipEntry((uint)newTopId, 8)]
            );
            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            Assert.NotNull(session.Character);
            Assert.NotSame(staleCharacter, session.Character);
            Assert.Contains(session.Character!.Equipment, e => e.ItemId == newTopId);
            Assert.DoesNotContain(session.Character.Equipment, e => e.ItemId == oldTopId);
            Assert.Contains(
                session.Character.Inventory,
                i => i.ItemId == oldTopId && i.Quantity == 1
            );

            Assert.Contains(
                session.Sent,
                packet => packet.Type == PacketType.ItemTryEquipReplaceResponse
            );
            Assert.Contains(
                session.Sent,
                packet => packet.Type == PacketType.ItemTryEquipReplacedNotify
            );
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

                db.Items.AddRange(
                    new Item
                    {
                        Id = oldTopId,
                        Name = "Old Top",
                        Socket = 8,
                    },
                    new Item
                    {
                        Id = newTopId,
                        Name = "New Top",
                        Socket = 8,
                    }
                );
                db.CharacterEquipments.Add(
                    new CharacterEquipment
                    {
                        CharacterId = characterId,
                        SlotIndex = 1,
                        ItemId = oldTopId,
                    }
                );
                db.CharacterInventories.Add(
                    new CharacterInventory
                    {
                        CharacterId = characterId,
                        ItemId = newTopId,
                        Quantity = 1,
                    }
                );

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

            var repo = new CharacterRepository(
                new MainContext(options),
                NullLogger<CharacterRepository>.Instance
            );
            var roboRepository = new RoboRepository(new MainContext(options));
            var handler = new ItemTryEquipReplaceHandler(
                repo,
                roboRepository,
                state,
                NullLogger<ItemTryEquipReplaceHandler>.Instance
            );

            var payload = BuildReplaceRequestPayload(
                objId: (uint)characterId,
                equips: [new ItemEquipEntry((uint)newTopId, 8)]
            );
            await handler.HandleAsync(payload, actor, TestContext.Current.CancellationToken);

            Assert.Contains(peer.Sent, packet => packet.Type == PacketType.AvatarNotifyData);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_RoboTargetUpdatesOnlyRoboEquipmentAndBroadcastsRoboUpdate()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int characterId = 12003;
            const int oldAvatarTopId = 10100060;
            const int oldRoboTopId = 10100100;
            const int newRoboTopId = 10100220;
            var roboObjectId = RoboRepository.GetObjectId(characterId, 1);

            await TestDb.SeedCharacterAsync(
                options,
                characterId,
                TestContext.Current.CancellationToken
            );
            await using (var db = new MainContext(options))
            {
                db.Items.AddRange(
                    new Item
                    {
                        Id = oldAvatarTopId,
                        Name = "Avatar Top",
                        Socket = 8,
                    },
                    new Item
                    {
                        Id = oldRoboTopId,
                        Name = "Old Robo Top",
                        Socket = 8,
                    },
                    new Item
                    {
                        Id = newRoboTopId,
                        Name = "New Robo Top",
                        Socket = 8,
                    }
                );
                db.CharacterEquipments.Add(
                    new CharacterEquipment
                    {
                        CharacterId = characterId,
                        SlotIndex = 0,
                        ItemId = oldAvatarTopId,
                    }
                );
                db.CharacterInventories.Add(
                    new CharacterInventory
                    {
                        CharacterId = characterId,
                        ItemId = newRoboTopId,
                        Quantity = 1,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                var roboCharacter = new CharaData(roboObjectId, 1002011, "Wardrobe Robo");
                roboCharacter.AddEquip((uint)oldRoboTopId, 8);
                await new RoboRepository(db).UpsertAsync(
                    characterId,
                    new RoboData(1, roboCharacter) { OwnerAvatarId = characterId },
                    TestContext.Current.CancellationToken
                );
            }

            var state = new SharedState();
            var actor = new CapturingPlayerSession
            {
                CharacterId = characterId,
                MapId = 20000000,
                MyRoomId = characterId,
                ChannelId = 1,
            };
            var peer = new CapturingPlayerSession
            {
                CharacterId = 22003,
                MapId = 20000000,
                MyRoomId = characterId,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, actor);
            state.RegisterClient(ServerType.Area, peer);

            var characterRepository = new CharacterRepository(
                new MainContext(options),
                NullLogger<CharacterRepository>.Instance
            );
            var roboRepository = new RoboRepository(new MainContext(options));
            var handler = new ItemTryEquipReplaceHandler(
                characterRepository,
                roboRepository,
                state,
                NullLogger<ItemTryEquipReplaceHandler>.Instance
            );

            await handler.HandleAsync(
                BuildReplaceRequestPayload(roboObjectId, [new ItemEquipEntry(newRoboTopId, 8)]),
                actor,
                TestContext.Current.CancellationToken
            );

            await using (var verificationDb = new MainContext(options))
            {
                var avatarEquipment = await verificationDb.CharacterEquipments.SingleAsync(
                    TestContext.Current.CancellationToken
                );
                Assert.Equal(oldAvatarTopId, avatarEquipment.ItemId);

                var storedRobo = await new RoboRepository(verificationDb).GetAsync(
                    characterId,
                    1,
                    TestContext.Current.CancellationToken
                );
                Assert.NotNull(storedRobo);
                Assert.Equal((uint)newRoboTopId, storedRobo.Character.Equips[0].ItemId);
                Assert.All(
                    storedRobo.Character.Equips.Skip(1),
                    equip => Assert.Equal(0u, equip.ItemId)
                );

                // Old Robo clothing returned to inventory; newly equipped piece consumed.
                Assert.Equal(
                    1,
                    await verificationDb
                        .CharacterInventories.Where(i =>
                            i.CharacterId == characterId && i.ItemId == oldRoboTopId
                        )
                        .Select(i => i.Quantity)
                        .SingleAsync(TestContext.Current.CancellationToken)
                );
                Assert.False(
                    await verificationDb.CharacterInventories.AnyAsync(
                        i => i.CharacterId == characterId && i.ItemId == newRoboTopId,
                        TestContext.Current.CancellationToken
                    )
                );
            }

            Assert.DoesNotContain(
                actor.Sent,
                packet =>
                    packet.Type
                        is PacketType.ItemEquippedNotify
                            or PacketType.ItemRemovedNotify
                            or PacketType.AvatarNotifyData
            );
            Assert.Equal(PacketType.ItemTryEquipReplaceResponse, actor.Sent[0].Type);
            Assert.Equal(PacketType.ItemTryEquipReplacedNotify, actor.Sent[1].Type);
            Assert.Equal(roboObjectId, new PacketReader(actor.Sent[1].Payload).ReadUInt());
            Assert.Contains(actor.Sent, packet => packet.Type == PacketType.ItemCreateNotify);
            Assert.Contains(actor.Sent, packet => packet.Type == PacketType.ItemUpdateListNotify);
            AssertRoboEquipmentUpdate(actor.Sent[^1], roboObjectId, newRoboTopId);
            Assert.Collection(
                peer.Sent,
                packet => AssertRoboEquipmentUpdate(packet, roboObjectId, newRoboTopId)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_RoboUnequipReturnsItemsToCharacterInventory()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int characterId = 12004;
            const int roboTopId = 10100100;
            var roboObjectId = RoboRepository.GetObjectId(characterId, 1);

            await TestDb.SeedCharacterAsync(
                options,
                characterId,
                TestContext.Current.CancellationToken
            );
            await using (var db = new MainContext(options))
            {
                db.Items.Add(
                    new Item
                    {
                        Id = roboTopId,
                        Name = "Robo Top",
                        Socket = 8,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                var roboCharacter = new CharaData(roboObjectId, 1002011, "Unequip Robo");
                roboCharacter.AddEquip((uint)roboTopId, 8);
                await new RoboRepository(db).UpsertAsync(
                    characterId,
                    new RoboData(1, roboCharacter) { OwnerAvatarId = characterId },
                    TestContext.Current.CancellationToken
                );
            }

            var state = new SharedState();
            var actor = new CapturingPlayerSession
            {
                CharacterId = characterId,
                MapId = 20000000,
                MyRoomId = characterId,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, actor);

            var handler = new ItemTryEquipReplaceHandler(
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new RoboRepository(new MainContext(options)),
                state,
                NullLogger<ItemTryEquipReplaceHandler>.Instance
            );

            // Empty equip list = strip all clothing from the Charadoll.
            await handler.HandleAsync(
                BuildReplaceRequestPayload(roboObjectId, []),
                actor,
                TestContext.Current.CancellationToken
            );

            await using (var verificationDb = new MainContext(options))
            {
                var storedRobo = await new RoboRepository(verificationDb).GetAsync(
                    characterId,
                    1,
                    TestContext.Current.CancellationToken
                );
                Assert.NotNull(storedRobo);
                Assert.All(storedRobo.Character.Equips, equip => Assert.Equal(0u, equip.ItemId));

                Assert.Equal(
                    1,
                    await verificationDb
                        .CharacterInventories.Where(i =>
                            i.CharacterId == characterId && i.ItemId == roboTopId
                        )
                        .Select(i => i.Quantity)
                        .SingleAsync(TestContext.Current.CancellationToken)
                );
            }

            Assert.Contains(actor.Sent, packet => packet.Type == PacketType.ItemCreateNotify);
            Assert.Contains(actor.Sent, packet => packet.Type == PacketType.ItemUpdateListNotify);
            Assert.DoesNotContain(
                actor.Sent,
                packet =>
                    packet.Type is PacketType.ItemEquippedNotify or PacketType.ItemRemovedNotify
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Reset_RoboTargetRestoresPersistedRoboEquipment()
    {
        const uint characterId = 42;
        const uint roboId = 1;
        const uint topId = 10100220;
        var roboObjectId = RoboRepository.GetObjectId(characterId, roboId);
        var roboCharacter = new CharaData(roboObjectId, 1002011, "Reset Robo");
        roboCharacter.AddEquip(topId, 8);
        for (var slot = 1; slot < CharaData.EquipmentSlotCount; slot++)
            roboCharacter.AddEquip(0, 0);

        var characterRepository = new Mock<ICharacterRepository>();
        var roboRepository = new Mock<IRoboRepository>();
        roboRepository
            .Setup(x =>
                x.GetAsync(checked((int)characterId), roboId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new RoboData(roboId, roboCharacter) { OwnerAvatarId = characterId });

        var handler = new ItemTryEquipResetHandler(
            characterRepository.Object,
            roboRepository.Object,
            NullLogger<ItemTryEquipResetHandler>.Instance
        );
        var session = new CapturingPlayerSession { CharacterId = characterId };

        await handler.HandleAsync(
            BuildSingleUIntPayload(roboObjectId),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Collection(
            session.Sent,
            packet =>
            {
                Assert.Equal(PacketType.ItemTryEquipResetResponse, packet.Type);
                Assert.Equal(0u, new PacketReader(packet.Payload).ReadUInt());
            },
            packet =>
            {
                Assert.Equal(PacketType.ItemTryEquipReplacedNotify, packet.Type);
                var reader = new PacketReader(packet.Payload);
                Assert.Equal(roboObjectId, reader.ReadUInt());
                Assert.Equal((uint)CharaData.EquipmentSlotCount, reader.ReadUInt());
                Assert.Equal(topId, reader.ReadUInt());
                Assert.Equal(8u, reader.ReadUInt());
            }
        );
    }

    [Fact]
    public async Task WardrobeControlHandlers_AcceptOwnedRoboTarget()
    {
        const uint characterId = 42;
        const uint roboId = 1;
        var roboObjectId = RoboRepository.GetObjectId(characterId, roboId);
        var roboRepository = new Mock<IRoboRepository>();
        roboRepository
            .Setup(x =>
                x.ExistsAsync(checked((int)characterId), roboId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);
        var session = new CapturingPlayerSession { CharacterId = characterId };
        var payload = BuildSingleUIntPayload(roboObjectId);

        var startHandler = new ItemEquipStartHandler(
            roboRepository.Object,
            NullLogger<ItemEquipStartHandler>.Instance
        );
        await startHandler.HandleAsync(payload, session, TestContext.Current.CancellationToken);
        Assert.Collection(
            session.Sent,
            packet =>
            {
                Assert.Equal(PacketType.ItemEquipStartResponse, packet.Type);
                Assert.Equal(1u, new PacketReader(packet.Payload).ReadUInt());
            },
            packet => AssertObjectPacket(packet, PacketType.ItemEquipStarted, roboObjectId),
            packet => AssertObjectPacket(packet, PacketType.ItemEquipForceStarted, roboObjectId)
        );

        session.Sent.Clear();
        var fixHandler = new ItemTryEquipFixHandler(
            roboRepository.Object,
            NullLogger<ItemTryEquipFixHandler>.Instance
        );
        await fixHandler.HandleAsync(payload, session, TestContext.Current.CancellationToken);
        Assert.Collection(
            session.Sent,
            packet =>
            {
                Assert.Equal(PacketType.ItemTryEquipFixResponse, packet.Type);
                Assert.Equal(0u, new PacketReader(packet.Payload).ReadUInt());
            }
        );

        session.Sent.Clear();
        var endHandler = new ItemEquipEndHandler(
            roboRepository.Object,
            NullLogger<ItemEquipEndHandler>.Instance
        );
        await endHandler.HandleAsync(payload, session, TestContext.Current.CancellationToken);
        Assert.Collection(
            session.Sent,
            packet =>
            {
                Assert.Equal(PacketType.ItemEquipEndResponse, packet.Type);
                Assert.Equal(0u, new PacketReader(packet.Payload).ReadUInt());
            },
            packet => AssertObjectPacket(packet, PacketType.ItemEquipEnded, roboObjectId)
        );
    }

    [Fact]
    public async Task WardrobeControlHandlers_RejectUnownedRoboTarget()
    {
        const uint characterId = 42;
        var unownedObjectId = RoboRepository.GetObjectId(43, 1);
        var roboRepository = new Mock<IRoboRepository>();
        var session = new CapturingPlayerSession { CharacterId = characterId };
        var payload = BuildSingleUIntPayload(unownedObjectId);

        await new ItemEquipStartHandler(
            roboRepository.Object,
            NullLogger<ItemEquipStartHandler>.Instance
        ).HandleAsync(payload, session, TestContext.Current.CancellationToken);
        Assert.Collection(
            session.Sent,
            packet =>
            {
                Assert.Equal(PacketType.ItemEquipStartResponse, packet.Type);
                Assert.Equal(0u, new PacketReader(packet.Payload).ReadUInt());
            }
        );

        session.Sent.Clear();
        await new ItemTryEquipFixHandler(
            roboRepository.Object,
            NullLogger<ItemTryEquipFixHandler>.Instance
        ).HandleAsync(payload, session, TestContext.Current.CancellationToken);
        Assert.Collection(
            session.Sent,
            packet =>
            {
                Assert.Equal(PacketType.ItemTryEquipFixResponse, packet.Type);
                Assert.Equal(1u, new PacketReader(packet.Payload).ReadUInt());
            }
        );

        session.Sent.Clear();
        await new ItemEquipEndHandler(
            roboRepository.Object,
            NullLogger<ItemEquipEndHandler>.Instance
        ).HandleAsync(payload, session, TestContext.Current.CancellationToken);
        Assert.Collection(
            session.Sent,
            packet =>
            {
                Assert.Equal(PacketType.ItemEquipEndResponse, packet.Type);
                Assert.Equal(1u, new PacketReader(packet.Payload).ReadUInt());
            }
        );
    }

    private static byte[] BuildReplaceRequestPayload(
        uint objId,
        IReadOnlyList<ItemEquipEntry> equips
    )
    {
        var writer = new PacketWriter();
        writer.Write(objId);
        writer.Write((uint)equips.Count);
        foreach (var equip in equips)
            writer.Write(equip.ToBytes());
        return writer.ToBytes();
    }

    private static byte[] BuildSingleUIntPayload(uint value)
    {
        var writer = new PacketWriter();
        writer.Write(value);
        return writer.ToBytes();
    }

    private static void AssertObjectPacket(
        (PacketType Type, byte[] Payload) packet,
        PacketType expectedType,
        uint expectedObjectId
    )
    {
        Assert.Equal(expectedType, packet.Type);
        Assert.Equal(expectedObjectId, new PacketReader(packet.Payload).ReadUInt());
    }

    private static void AssertRoboEquipmentUpdate(
        (PacketType Type, byte[] Payload) packet,
        uint expectedObjectId,
        uint expectedTopId
    )
    {
        Assert.Equal(PacketType.NotifyUpdateRoboEquip, packet.Type);
        var reader = new PacketReader(packet.Payload);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(expectedObjectId, reader.ReadUInt());
        Assert.Equal((uint)CharaData.EquipmentSlotCount, reader.ReadUInt());
        Assert.Equal(expectedTopId, reader.ReadUInt());
        Assert.Equal(8u, reader.ReadUInt());
        Assert.Equal(12 + CharaData.EquipmentSlotCount * 8, packet.Payload.Length);
    }
}
