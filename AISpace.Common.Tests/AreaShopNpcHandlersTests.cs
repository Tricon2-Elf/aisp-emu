using AISpace.Common.Config;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Tests;

public class AreaShopNpcHandlersTests
{
    private const uint StarterMapId = 10990100;
    private const uint StarterNpcObjectId = 0x50000001;
    private const uint StarterModelId = 1001021;
    private const int StarterItemId = 10100220;

    [Fact]
    public async Task NpcGetDataHandler_SendsConfiguredNpcFromDatabase()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedShopNpcAsync(
                    db,
                    isNpcEnabled: true,
                    isShopEnabled: true,
                    isShopItemEnabled: true
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var handler = new AreaNpcGetDataHandler(new NpcRepository(runDb));
            var session = new CapturingPlayerSession { MapId = StarterMapId };

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Contains(session.Sent, p => p.Type == PacketType.NpcGetDataResponse);
            var npcPacket = Assert.Single(session.Sent, p => p.Type == PacketType.NpcNotifyData);
            var reader = new PacketReader(npcPacket.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(StarterNpcObjectId, reader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventAccessNpcHandler_UsesDatabaseShopBannerAndItems()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const uint bannerVisualId = 10110;

            await using (var db = new MainContext(options))
            {
                await SeedShopNpcAsync(
                    db,
                    isNpcEnabled: true,
                    isShopEnabled: true,
                    isShopItemEnabled: true,
                    bannerVisualId: bannerVisualId
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var handler = CreateEventAccessNpcHandler(runDb);
            var session = new CapturingPlayerSession { MapId = StarterMapId, CharacterId = 9001 };

            await handler.HandleAsync(
                BuildEventAccessNpcPayload(StarterNpcObjectId),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Contains(session.Sent, p => p.Type == PacketType.EventAccessNpcResponse);
            Assert.Contains(session.Sent, p => p.Type == PacketType.NotifySupplyNpcExec);
            var shopStarted = Assert.Single(
                session.Sent,
                p => p.Type == PacketType.ShopStartedNotify
            );
            var startedReader = new PacketReader(shopStarted.Payload);
            Assert.Equal(StarterNpcObjectId, startedReader.ReadUInt());
            Assert.Equal("Starter Shop", startedReader.ReadString("ASCII"));
            Assert.Equal(bannerVisualId, startedReader.ReadUInt());
            Assert.Equal(0u, startedReader.ReadUInt());

            var shopItems = Assert.Single(session.Sent, p => p.Type == PacketType.ShopItemNotify);
            var itemReader = new PacketReader(shopItems.Payload);
            Assert.Equal(1u, itemReader.ReadUInt());
            Assert.Equal(50UL, itemReader.ReadULong());
            Assert.Equal(50UL, itemReader.ReadULong());
            Assert.Equal((uint)StarterItemId, itemReader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventAccessNpcHandler_StartsClientScript_WhenNpcHasEventKey()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                db.Npcs.Add(
                    new Npc
                    {
                        MapId = StarterMapId,
                        ChannelId = -1,
                        DayPhase = -1,
                        DateStartUtc = DateTime.UnixEpoch,
                        DateEndUtc = DateTime.MaxValue,
                        NpcObjectId = 1342177288,
                        ModelId = 3992011,
                        Name = "Rin",
                        X = -9200f,
                        Y = 2f,
                        Z = -16887f,
                        Rotation = 90,
                        InteractionType = NpcInteractionType.Decorative,
                        EventKind = NpcEventKind.ClientScript,
                        EventKey = ScriptedEvents.Keys.IntroductionRin02,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var handler = CreateEventAccessNpcHandler(runDb);
            var session = new CapturingPlayerSession
            {
                MapId = StarterMapId,
                CharacterId = 9001,
                ActiveShopId = 42,
            };

            await handler.HandleAsync(
                BuildEventAccessNpcPayload(1342177288),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Null(session.ActiveShopId);
            Assert.Equal(ScriptedEvents.Keys.IntroductionRin02, session.ActiveEventKey);
            Assert.Equal(NpcEventKind.ClientScript, session.ActiveEventKind);
            Assert.Equal(EventCompletionPolicy.Replayable, session.ActiveEventCompletionPolicy);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventAccessNpcResponse);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventStartNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventScriptPlayNotify);
            var script = session.Sent.Single(p => p.Type == PacketType.EventScriptPlayNotify);
            Assert.Equal(
                new EventScriptPlayNotify(
                    ScriptedEvents.GetScriptLabel(ScriptedEvents.Keys.IntroductionRin02)
                ).ToBytes(),
                script.Payload
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventAccessNpcHandler_Rejects_WhenAlreadyInEvent()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                db.Npcs.Add(
                    new Npc
                    {
                        MapId = StarterMapId,
                        ChannelId = -1,
                        DayPhase = -1,
                        DateStartUtc = DateTime.UnixEpoch,
                        DateEndUtc = DateTime.MaxValue,
                        NpcObjectId = 1342177288,
                        ModelId = 3992011,
                        Name = "Rin",
                        InteractionType = NpcInteractionType.Decorative,
                        EventKind = NpcEventKind.ClientScript,
                        EventKey = ScriptedEvents.Keys.IntroductionRin02,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var handler = CreateEventAccessNpcHandler(runDb);
            var session = new CapturingPlayerSession
            {
                MapId = StarterMapId,
                CharacterId = 9001,
                ActiveEventKey = ScriptedEvents.Keys.IntroductionRin01,
                ActiveEventKind = NpcEventKind.ClientScript,
                ActiveEventCompletionPolicy = EventCompletionPolicy.Once,
            };

            await handler.HandleAsync(
                BuildEventAccessNpcPayload(1342177288),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(ScriptedEvents.Keys.IntroductionRin01, session.ActiveEventKey);
            var response = Assert.Single(
                session.Sent,
                p => p.Type == PacketType.EventAccessNpcResponse
            );
            Assert.Equal(1u, new PacketReader(response.Payload).ReadUInt());
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.EventStartNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventAccessNpcHandler_RejectsUnknownServerScript()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                db.Npcs.Add(
                    new Npc
                    {
                        MapId = StarterMapId,
                        ChannelId = -1,
                        DayPhase = -1,
                        DateStartUtc = DateTime.UnixEpoch,
                        DateEndUtc = DateTime.MaxValue,
                        NpcObjectId = 1342177299,
                        ModelId = 1,
                        Name = "Broken",
                        InteractionType = NpcInteractionType.Decorative,
                        EventKind = NpcEventKind.ServerScript,
                        EventKey = "missing_server_script",
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var handler = CreateEventAccessNpcHandler(runDb);
            var session = new CapturingPlayerSession
            {
                MapId = StarterMapId,
                CharacterId = 9001,
                ActiveShopId = 42,
            };

            await handler.HandleAsync(
                BuildEventAccessNpcPayload(1342177299),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Null(session.ActiveShopId);
            Assert.Null(session.ActiveEventKey);
            var response = Assert.Single(
                session.Sent,
                p => p.Type == PacketType.EventAccessNpcResponse
            );
            Assert.Equal(1u, new PacketReader(response.Payload).ReadUInt());
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.EventStartNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventAccessNpcHandler_RejectsDisabledNpc()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedShopNpcAsync(
                    db,
                    isNpcEnabled: false,
                    isShopEnabled: true,
                    isShopItemEnabled: true
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var handler = CreateEventAccessNpcHandler(runDb);
            var session = new CapturingPlayerSession { MapId = StarterMapId, CharacterId = 9001 };

            await handler.HandleAsync(
                BuildEventAccessNpcPayload(StarterNpcObjectId),
                session,
                TestContext.Current.CancellationToken
            );

            var response = Assert.Single(
                session.Sent,
                p => p.Type == PacketType.EventAccessNpcResponse
            );
            var reader = new PacketReader(response.Payload);
            Assert.Equal(1u, reader.ReadUInt());
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.ShopStartedNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task NpcHandlers_RespectChannelRestrictions()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedShopNpcAsync(
                    db,
                    isNpcEnabled: true,
                    isShopEnabled: true,
                    isShopItemEnabled: true,
                    channelId: 1
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var npcHandler = new AreaNpcGetDataHandler(new NpcRepository(runDb));
            var accessHandler = CreateEventAccessNpcHandler(runDb);

            var offChannelSession = new CapturingPlayerSession
            {
                MapId = StarterMapId,
                ChannelId = 2,
                CharacterId = 9001,
            };

            await npcHandler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                offChannelSession,
                TestContext.Current.CancellationToken
            );
            Assert.DoesNotContain(offChannelSession.Sent, p => p.Type == PacketType.NpcNotifyData);

            await accessHandler.HandleAsync(
                BuildEventAccessNpcPayload(StarterNpcObjectId),
                offChannelSession,
                TestContext.Current.CancellationToken
            );
            var response = Assert.Single(
                offChannelSession.Sent,
                p => p.Type == PacketType.EventAccessNpcResponse
            );
            var reader = new PacketReader(response.Payload);
            Assert.Equal(1u, reader.ReadUInt());
            Assert.DoesNotContain(
                offChannelSession.Sent,
                p => p.Type == PacketType.ShopStartedNotify
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static async Task SeedShopNpcAsync(
        MainContext db,
        bool isNpcEnabled,
        bool isShopEnabled,
        bool isShopItemEnabled,
        uint bannerVisualId = 10110,
        int channelId = -1
    )
    {
        db.Items.Add(
            new Item
            {
                Id = StarterItemId,
                Name = "Starter Item",
                Socket = 8,
            }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var shop = new Shop
        {
            Code = "starter_clothing",
            DisplayName = "Starter Shop",
            BannerVisualId = bannerVisualId,
            IsEnabled = isShopEnabled,
        };
        db.Shops.Add(shop);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.ShopItems.Add(
            new ShopItem
            {
                ShopId = shop.Id,
                ItemId = StarterItemId,
                AiPrice = 50,
                NicoPrice = 50,
                SortOrder = 0,
                IsEnabled = isShopItemEnabled,
            }
        );

        var npc = new Npc
        {
            MapId = StarterMapId,
            ChannelId = channelId,
            DayPhase = -1,
            DateStartUtc = DateTime.UnixEpoch,
            DateEndUtc = DateTime.MaxValue,
            NpcObjectId = StarterNpcObjectId,
            ModelId = StarterModelId,
            Name = "Starter Shop NPC",
            X = -9000f,
            Y = 2f,
            Z = -17900f,
            Rotation = 90,
            ShopId = shop.Id,
            InteractionType = NpcInteractionType.Shop,
            IsEnabled = isNpcEnabled,
            SortOrder = 0,
        };
        db.Npcs.Add(npc);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.NpcEquipments.Add(
            new NpcEquipment
            {
                NpcId = npc.Id,
                SlotIndex = 0,
                ItemId = StarterItemId,
                SortOrder = 0,
            }
        );
    }

    [Fact]
    public async Task EventAccessNpcHandler_StartsServerScript_ForShinjuNpc()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedFranchiseHubMapsAsync(db);
                db.Users.Add(new User { Id = 1, Username = "tester" });
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                var user = db.Users.Single();
                db.Characters.Add(
                    new Character
                    {
                        Name = "Tester",
                        UserId = user.Id,
                        ModelId = 1,
                        Birthdate = DateTime.UnixEpoch,
                    }
                );
                db.Npcs.Add(
                    new Npc
                    {
                        MapId = 10990300,
                        ChannelId = -1,
                        DayPhase = -1,
                        DateStartUtc = DateTime.UnixEpoch,
                        DateEndUtc = DateTime.MaxValue,
                        NpcObjectId = 1342177291,
                        ModelId = 3992031,
                        Name = "Shinju",
                        X = 348.20844f,
                        Y = 0.009996034f,
                        Z = -327.27664f,
                        Rotation = 0,
                        InteractionType = NpcInteractionType.Decorative,
                        EventKind = NpcEventKind.ServerScript,
                        EventKey = ServerEvents.Keys.ShinjuRegistration,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var characterId = runDb.Characters.Select(c => c.Id).Single();
            var handler = CreateEventAccessNpcHandler(runDb);
            var session = new CapturingPlayerSession
            {
                MapId = 10990300,
                CharacterId = (uint)characterId,
            };

            await handler.HandleAsync(
                BuildEventAccessNpcPayload(1342177291),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(ServerEvents.Keys.ShinjuRegistration, session.ActiveEventKey);
            Assert.Equal(NpcEventKind.ServerScript, session.ActiveEventKind);
            Assert.Equal(EventCompletionPolicy.Once, session.ActiveEventCompletionPolicy);
            Assert.NotNull(session.ServerScriptState);
            Assert.Equal("IslandSelect", session.ServerScriptState!.Step);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventAccessNpcResponse);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventStartNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventMessageNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventMessageCloseNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.SelectInitIslandStart);
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.EventEndNotify);

            var islandStart = OutgoingPacketTestParsers.ParseSelectInitIslandStartNotify(
                session.Sent.Single(p => p.Type == PacketType.SelectInitIslandStart).Payload
            );
            Assert.Equal(3, islandStart.Islands.Count);
            Assert.Equal(
                4 + (SelectInitIslandEntry.PacketSize * 3),
                session.Sent.Single(p => p.Type == PacketType.SelectInitIslandStart).Payload.Length
            );
            Assert.DoesNotContain(
                session.Sent,
                p => p.Type == PacketType.EventIslandSelectExecNotify
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SelectInitIslandEndHandler_ContinuesToCharadollSelect_AfterIslandSelection()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedFranchiseHubMapsAsync(db);
                db.Users.Add(new User { Id = 1, Username = "tester" });
                db.Npcs.Add(
                    new Npc
                    {
                        MapId = 10990300,
                        ChannelId = -1,
                        DayPhase = -1,
                        DateStartUtc = DateTime.UnixEpoch,
                        DateEndUtc = DateTime.MaxValue,
                        NpcObjectId = 1342177291,
                        ModelId = 3992031,
                        Name = "Shinju",
                        IsEnabled = true,
                    }
                );
                db.Characters.Add(
                    new Character
                    {
                        Name = "Tester",
                        UserId = 1,
                        ModelId = 1,
                        Birthdate = DateTime.UnixEpoch,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var characterId = runDb.Characters.Select(c => c.Id).Single();
            var shinjuNpc = runDb.Npcs.Single();
            var dispatcher = CreateServerScriptDispatcher(runDb);
            var handler = new AreaSelectInitIslandEndHandler(
                CreateDirectMapLinkTransitionService(runDb, new SharedState()),
                dispatcher,
                NullLogger<AreaSelectInitIslandEndHandler>.Instance
            );
            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)characterId,
                ActiveEventKey = ServerEvents.Keys.ShinjuRegistration,
                ActiveEventKind = NpcEventKind.ServerScript,
                ActiveEventCompletionPolicy = EventCompletionPolicy.Once,
                ServerScriptState = new ServerScriptState
                {
                    EventKey = ServerEvents.Keys.ShinjuRegistration,
                    Step = "IslandSelect",
                },
            };
            session.ServerScriptState.Data["npc"] = shinjuNpc;

            await handler.HandleAsync(
                OutgoingPacketTestParsers.SelectInitIslandEndRequestToBytes(
                    new SelectInitIslandEndRequest { IslandId = 3 }
                ),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(ServerEvents.Keys.ShinjuRegistration, session.ActiveEventKey);
            Assert.Equal("CharadollSelect", session.ServerScriptState!.Step);
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.EventStartNotify
            );
            Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
            Assert.Contains(
                session.Sent,
                packet => packet.Type == PacketType.EventSelectInitNotify
            );
            Assert.Equal(
                3,
                session.Sent.Count(packet => packet.Type == PacketType.EventSelectPushNotify)
            );
            Assert.Contains(
                session.Sent,
                packet => packet.Type == PacketType.EventSelectExecNotify
            );

            var character = await runDb.Characters.SingleAsync(
                TestContext.Current.CancellationToken
            );
            Assert.Equal(3u, character.HomeIslandId);
            Assert.Equal(1u, character.ModelId);
            Assert.False(
                await runDb.CharacterEventStatuses.AnyAsync(
                    x =>
                        x.CharacterId == characterId
                        && x.EventKey == ServerEvents.Keys.ShinjuRegistration,
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
    public async Task EventSelectExecRHandler_CompletesRegistration_AfterCharadollSelection()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedFranchiseHubMapsAsync(db);
                db.Users.Add(new User { Id = 1, Username = "tester" });
                db.Characters.Add(
                    new Character
                    {
                        Name = "Tester",
                        UserId = 1,
                        ModelId = 1,
                        HomeIslandId = 3,
                        Birthdate = DateTime.UnixEpoch,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var characterId = runDb.Characters.Select(c => c.Id).Single();
            var dispatcher = CreateServerScriptDispatcher(runDb);
            var handler = new AreaEventSelectExecRHandler(
                dispatcher,
                NullLogger<AreaEventSelectExecRHandler>.Instance
            );
            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)characterId,
                ActiveEventKey = ServerEvents.Keys.ShinjuRegistration,
                ActiveEventKind = NpcEventKind.ServerScript,
                ActiveEventCompletionPolicy = EventCompletionPolicy.Once,
                ServerScriptState = new ServerScriptState
                {
                    EventKey = ServerEvents.Keys.ShinjuRegistration,
                    Step = "CharadollSelect",
                },
            };
            session.ServerScriptState.Data["islandId"] = 3u;

            var writer = new PacketWriter();
            writer.Write(0u);
            writer.Write((byte)1);
            await handler.HandleAsync(
                writer.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Null(session.ActiveEventKey);
            Assert.Equal(NpcEventKind.None, session.ActiveEventKind);
            Assert.Null(session.ServerScriptState);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventEndNotify);

            var character = await runDb.Characters.SingleAsync(
                TestContext.Current.CancellationToken
            );
            Assert.Equal(3u, character.HomeIslandId);
            Assert.Equal(1u, character.ModelId);
            Assert.Equal(CharadollPersonality.Quiet, character.CharadollPersonality);
            Assert.True(
                await runDb.CharacterEventStatuses.AnyAsync(
                    x =>
                        x.CharacterId == characterId
                        && x.EventKey == ServerEvents.Keys.ShinjuRegistration,
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
    public async Task EventAccessNpcHandler_ResumesCharadollSelect_WhenHomeIslandAlreadySaved()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedFranchiseHubMapsAsync(db);
                db.Users.Add(new User { Id = 1, Username = "tester" });
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                var user = db.Users.Single();
                db.Characters.Add(
                    new Character
                    {
                        Name = "Tester",
                        UserId = user.Id,
                        ModelId = 1,
                        HomeIslandId = 3,
                        Birthdate = DateTime.UnixEpoch,
                    }
                );
                db.Npcs.Add(
                    new Npc
                    {
                        MapId = 10990300,
                        ChannelId = -1,
                        DayPhase = -1,
                        DateStartUtc = DateTime.UnixEpoch,
                        DateEndUtc = DateTime.MaxValue,
                        NpcObjectId = 1342177291,
                        ModelId = 3992031,
                        Name = "Shinju",
                        InteractionType = NpcInteractionType.Decorative,
                        EventKind = NpcEventKind.ServerScript,
                        EventKey = ServerEvents.Keys.ShinjuRegistration,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var characterId = runDb.Characters.Select(c => c.Id).Single();
            var handler = CreateEventAccessNpcHandler(runDb);
            var session = new CapturingPlayerSession
            {
                MapId = 10990300,
                CharacterId = (uint)characterId,
            };

            await handler.HandleAsync(
                BuildEventAccessNpcPayload(1342177291),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(ServerEvents.Keys.ShinjuRegistration, session.ActiveEventKey);
            Assert.Equal("CharadollSelect", session.ServerScriptState!.Step);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventSelectInitNotify);
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.SelectInitIslandStart);
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.EventEndNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventAccessNpcHandler_ShowsHelpPrompt_WhenRegistrationAlreadyComplete()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedFranchiseHubMapsAsync(db);
                db.Users.Add(new User { Id = 1, Username = "tester" });
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                var user = db.Users.Single();
                db.Characters.Add(
                    new Character
                    {
                        Name = "Tester",
                        UserId = user.Id,
                        ModelId = 1,
                        HomeIslandId = 3,
                        CharadollPersonality = CharadollPersonality.Quiet,
                        Birthdate = DateTime.UnixEpoch,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                var characterId = db.Characters.Select(c => c.Id).Single();
                db.CharacterEventStatuses.Add(
                    new CharacterEventStatus
                    {
                        CharacterId = characterId,
                        EventKey = ServerEvents.Keys.ShinjuRegistration,
                        CompletedAtUtc = DateTime.UtcNow,
                    }
                );
                db.Npcs.Add(
                    new Npc
                    {
                        MapId = 10990300,
                        ChannelId = -1,
                        DayPhase = -1,
                        DateStartUtc = DateTime.UnixEpoch,
                        DateEndUtc = DateTime.MaxValue,
                        NpcObjectId = 1342177291,
                        ModelId = 3992031,
                        Name = "Shinju",
                        InteractionType = NpcInteractionType.Decorative,
                        EventKind = NpcEventKind.ServerScript,
                        EventKey = ServerEvents.Keys.ShinjuRegistration,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var characterIdForSession = runDb.Characters.Select(c => c.Id).Single();
            var dispatcher = CreateServerScriptDispatcher(runDb);
            var handler = new AreaEventAccessNpcHandler(
                new NpcRepository(runDb),
                new ShopRepository(runDb),
                dispatcher,
                NullLogger<AreaEventAccessNpcHandler>.Instance
            );
            var syncHandler = new AreaEventSyncRHandler(
                dispatcher,
                NullLogger<AreaEventSyncRHandler>.Instance
            );
            var session = new CapturingPlayerSession
            {
                MapId = 10990300,
                CharacterId = (uint)characterIdForSession,
            };

            await handler.HandleAsync(
                BuildEventAccessNpcPayload(1342177291),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(ServerEvents.Keys.ShinjuRegistration, session.ActiveEventKey);
            Assert.Equal(NpcEventKind.ServerScript, session.ActiveEventKind);
            Assert.Equal("HelpPrompt", session.ServerScriptState?.Step);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventStartNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventMessageCloseNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventSyncNotify);
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.EventEndNotify);
            var messagePayload = session
                .Sent.Single(p => p.Type == PacketType.EventMessageNotify)
                .Payload;
            var reader = new PacketReader(messagePayload);
            Assert.Equal(1342177291u, reader.ReadUInt());
            Assert.Equal("Shinju", reader.ReadString());
            Assert.Equal("Can I help you?", reader.ReadString());
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.SelectInitIslandStart);
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.EventSelectInitNotify);

            var syncPayload = new PacketWriter();
            syncPayload.Write(0u);
            await syncHandler.HandleAsync(
                syncPayload.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Null(session.ActiveEventKey);
            Assert.Equal(NpcEventKind.None, session.ActiveEventKind);
            Assert.Null(session.ServerScriptState);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventEndNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static AreaEventAccessNpcHandler CreateEventAccessNpcHandler(MainContext db) =>
        new(
            new NpcRepository(db),
            new ShopRepository(db),
            CreateServerScriptDispatcher(db),
            NullLogger<AreaEventAccessNpcHandler>.Instance
        );

    private static ServerScriptDispatcher CreateServerScriptDispatcher(MainContext db)
    {
        var eventRepository = new CharacterEventRepository(db);
        var serverScriptSession = new ServerScriptSession(
            eventRepository,
            NullLogger<ServerScriptSession>.Instance
        );
        var characterRepository = new CharacterRepository(
            db,
            NullLogger<CharacterRepository>.Instance
        );
        var mapRepository = new MapRepository(db);
        var shinjuRegistrationScript = new ShinjuRegistrationServerScript(
            characterRepository,
            eventRepository,
            mapRepository,
            serverScriptSession,
            NullLogger<ShinjuRegistrationServerScript>.Instance
        );
        return new ServerScriptDispatcher(
            [shinjuRegistrationScript],
            serverScriptSession,
            NullLogger<ServerScriptDispatcher>.Instance
        );
    }

    private static DirectMapLinkTransitionService CreateDirectMapLinkTransitionService(
        MainContext db,
        SharedState state
    ) =>
        new(
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
            NullLogger<DirectMapLinkTransitionService>.Instance
        );

    private static async Task SeedFranchiseHubMapsAsync(MainContext db)
    {
        db.Maps.AddRange(
            new Map
            {
                MapId = 10010100,
                Island = "Da Capo",
                Name = "Kazami Academy",
            },
            new Map
            {
                MapId = 10010200,
                Island = "Da Capo",
                Name = "Shopping Street",
            },
            new Map
            {
                MapId = 10020100,
                Island = "Clannad",
                Name = "Hikarizaka High School",
            },
            new Map
            {
                MapId = 10030100,
                Island = "Shuffle",
                Name = "Verbena Academy",
            }
        );
        await db.SaveChangesAsync();
    }

    private static byte[] BuildEventAccessNpcPayload(uint npcObjectId)
    {
        var writer = new PacketWriter();
        writer.Write(npcObjectId);
        writer.Write(-9000f);
        writer.Write(2f);
        writer.Write(-17900f);
        return writer.ToBytes();
    }
}
