using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class NiconiCommonsShopHandlersTests
{
    private const uint ClerkObjectId = 1342177339;
    private const uint MenTallGallantItemId = 14100000;

    [Fact]
    public async Task EventAccess_opens_the_commons_shop_with_24_figure_rows()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                db.Npcs.Add(
                    new Npc
                    {
                        MapId = 10990200,
                        ChannelId = -1,
                        DayPhase = -1,
                        DateStartUtc = DateTime.UnixEpoch,
                        DateEndUtc = DateTime.MaxValue,
                        NpcObjectId = ClerkObjectId,
                        ModelId = 1002031,
                        Name = "ニコニ・コモンズショップ",
                        X = -8100,
                        Y = 0,
                        Z = -11350,
                        Rotation = 180,
                        InteractionType = NpcInteractionType.NiconiCommonsShop,
                        IsEnabled = true,
                        SortOrder = 113,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            await DramaTestCatalog.SeedAsync(runDb);
            var handler = new AreaEventAccessNpcHandler(
                new NpcRepository(runDb),
                new ShopRepository(runDb),
                new ServerScriptDispatcher(
                    [],
                    new ServerScriptSession(
                        new CharacterEventRepository(runDb),
                        NullLogger<ServerScriptSession>.Instance
                    ),
                    NullLogger<ServerScriptDispatcher>.Instance
                ),
                new AdventureShopCatalog(new AdventureShopRepository(runDb)),
                TestTextLocaliser.English,
                NullLogger<AreaEventAccessNpcHandler>.Instance,
                new aisp.Common.Game.DramaCatalog(runDb, TestTextLocaliser.English)
            );
            var session = new CapturingPlayerSession { MapId = 10990200, CharacterId = 9001 };

            var access = new PacketWriter();
            access.Write(ClerkObjectId);
            access.Write(-8100f);
            access.Write(0f);
            access.Write(-11350f);
            await handler.HandleAsync(
                access.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.NotNull(session.ActiveShopId);
            Assert.DoesNotContain(
                session.Sent,
                x =>
                    x.Type
                        is PacketType.UccAdvFigureBaseListResponse
                            or PacketType.NiconiCommonsBaseListResponse
                            or PacketType.UccVoiceBaseListResponse
            );
            Assert.Equal(
                0u,
                new PacketReader(
                    session.Sent.Single(p => p.Type == PacketType.EventAccessNpcResponse).Payload
                ).ReadUInt()
            );
            Assert.Contains(session.Sent, p => p.Type == PacketType.NotifySupplyNpcExec);
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.MapEnterResponse);
            var started = Assert.Single(
                session.Sent,
                p => p.Type == PacketType.NiconiCommonsShopStartedNotify
            );
            var startedReader = new PacketReader(started.Payload);
            Assert.Equal(ClerkObjectId, startedReader.ReadUInt());
            Assert.False(string.IsNullOrEmpty(startedReader.ReadString()));
            Assert.Equal(0u, startedReader.ReadUInt());
            var items = Assert.Single(
                session.Sent,
                p => p.Type == PacketType.NiconiCommonsShopItemNotify
            );
            var itemReader = new PacketReader(items.Payload);
            Assert.Equal(10u, itemReader.ReadUInt());
            for (var i = 0; i < 10; i++)
            {
                Assert.Equal(i < 9 ? 2u : 3u, itemReader.ReadUInt());
                Assert.Equal(i < 9 ? 32001001u + (uint)i : 42001001u, itemReader.ReadUInt());
                Assert.Equal(100u, itemReader.ReadUInt());
                Assert.Equal(0u, itemReader.ReadUInt());
            }
            Assert.Equal(24u, itemReader.ReadUInt());
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.ShopStartedNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task BuyFigure_deducts_dere_puts_the_box_in_the_bag_and_obtains_the_picker_id()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int userId = 1;
            const int characterId = 8001;
            var user = new User
            {
                Id = userId,
                Username = "commons-buyer",
                AiPoints = 12000,
            };
            user.SetPassword("pw");
            user.Characters.Add(
                new Character
                {
                    Id = characterId,
                    Name = "Buyer",
                    UserId = userId,
                    Birthdate = new DateTime(2000, 1, 1),
                }
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Items.Add(
                    new Item
                    {
                        Id = (int)MenTallGallantItemId,
                        Name = "長身+りりしいモデル",
                        IconId = (int)MenTallGallantItemId,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            await DramaTestCatalog.SeedAsync(runDb);
            var session = new CapturingPlayerSession
            {
                User = user,
                UserId = userId,
                CharacterId = (uint)characterId,
                ActiveShopId = await runDb
                    .Shops.Where(x => x.Code == "niconi-commons")
                    .Select(x => x.Id)
                    .SingleAsync(TestContext.Current.CancellationToken),
            };
            var handler = new AreaNiconiCommonsShopBuyFigureHandler(
                runDb,
                new CharacterRepository(runDb, NullLogger<CharacterRepository>.Instance),
                NullLogger<AreaNiconiCommonsShopBuyFigureHandler>.Instance,
                new DramaCatalog(runDb, TestTextLocaliser.English)
            );

            foreach (var currency in new byte[] { 1, 2, 255 })
            {
                var disabled = new PacketWriter();
                disabled.Write(0x101u);
                disabled.Write(currency);
                await handler.HandleAsync(
                    disabled.ToBytes(),
                    session,
                    TestContext.Current.CancellationToken
                );
                Assert.Equal(
                    1u,
                    new PacketReader(
                        session
                            .Sent.Single(p => p.Type == PacketType.NiconiCommonsShopBuyResponse)
                            .Payload
                    ).ReadUInt()
                );
                Assert.DoesNotContain(
                    session.Sent,
                    p => p.Type == PacketType.UccAdvFigureObtainNotify
                );
                Assert.Equal(12000, session.User.AiPoints);
                session.Sent.Clear();
            }

            var payload = new PacketWriter();
            payload.Write(0x101u);
            payload.Write((byte)0);
            await handler.HandleAsync(
                payload.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            var buy = session.Sent.Single(p => p.Type == PacketType.NiconiCommonsShopBuyResponse);
            var buyReader = new PacketReader(buy.Payload);
            Assert.Equal(0u, buyReader.ReadUInt());
            Assert.Equal(7000ul, buyReader.ReadULong());
            Assert.Contains(session.Sent, p => p.Type == PacketType.MoneyUpdatedAipoint);

            var obtain = session.Sent.Single(p => p.Type == PacketType.UccAdvFigureObtainNotify);
            Assert.Equal(257u, new PacketReader(obtain.Payload).ReadUInt());

            session.Sent.Clear();
            await handler.HandleAsync(
                payload.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(
                1u,
                new PacketReader(
                    session
                        .Sent.Single(p => p.Type == PacketType.NiconiCommonsShopBuyResponse)
                        .Payload
                ).ReadUInt()
            );
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.UccAdvFigureObtainNotify);

            await using var verify = new MainContext(options);
            Assert.Equal(
                7000,
                (
                    await verify.Users.SingleAsync(
                        u => u.Id == userId,
                        TestContext.Current.CancellationToken
                    )
                ).AiPoints
            );
            Assert.Equal(
                1,
                (
                    await verify.CharacterInventories.SingleAsync(
                        i => i.CharacterId == characterId && i.ItemId == (int)MenTallGallantItemId,
                        TestContext.Current.CancellationToken
                    )
                ).Quantity
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task BuyFigure_refuses_when_broke_or_already_owned()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int userId = 2;
            const int characterId = 8002;
            var user = new User
            {
                Id = userId,
                Username = "broke",
                AiPoints = 50,
            };
            user.SetPassword("pw");
            user.Characters.Add(
                new Character
                {
                    Id = characterId,
                    Name = "Broke",
                    UserId = userId,
                    Birthdate = new DateTime(2000, 1, 1),
                }
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Items.Add(
                    new Item
                    {
                        Id = (int)MenTallGallantItemId,
                        Name = "長身+りりしいモデル",
                        IconId = (int)MenTallGallantItemId,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            await DramaTestCatalog.SeedAsync(runDb);
            var session = new CapturingPlayerSession
            {
                User = user,
                UserId = userId,
                CharacterId = (uint)characterId,
                ActiveShopId = await runDb
                    .Shops.Where(x => x.Code == "niconi-commons")
                    .Select(x => x.Id)
                    .SingleAsync(TestContext.Current.CancellationToken),
            };
            var handler = new AreaNiconiCommonsShopBuyFigureHandler(
                runDb,
                new CharacterRepository(runDb, NullLogger<CharacterRepository>.Instance),
                NullLogger<AreaNiconiCommonsShopBuyFigureHandler>.Instance,
                new DramaCatalog(runDb, TestTextLocaliser.English)
            );

            var payload = new PacketWriter();
            payload.Write(0x101u);
            await handler.HandleAsync(
                payload.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            var buy = session.Sent.Single(p => p.Type == PacketType.NiconiCommonsShopBuyResponse);
            Assert.Equal(1u, new PacketReader(buy.Payload).ReadUInt());
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.UccAdvFigureObtainNotify);

            await using var verify = new MainContext(options);
            Assert.Empty(
                await verify
                    .CharacterInventories.Where(i => i.CharacterId == characterId)
                    .ToListAsync(TestContext.Current.CancellationToken)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task BuyCommons_and_BuyVoice_deduct_dere_and_obtain()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int userId = 3;
            const int characterId = 8003;
            var user = new User
            {
                Id = userId,
                Username = "commons-tabs",
                AiPoints = 12000,
            };
            user.SetPassword("pw");
            user.Characters.Add(
                new Character
                {
                    Id = characterId,
                    Name = "Tabs",
                    UserId = userId,
                    Birthdate = new DateTime(2000, 1, 1),
                }
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Items.Add(
                    new Item
                    {
                        Id = 14100000,
                        Name = "長身+りりしいモデル",
                        IconId = 14100000,
                    }
                );
                db.Items.Add(
                    new Item
                    {
                        Id = 14100001,
                        Name = "長身+つり目なモデル",
                        IconId = 14100001,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            await DramaTestCatalog.SeedAsync(runDb);
            var session = new CapturingPlayerSession
            {
                User = user,
                UserId = userId,
                CharacterId = (uint)characterId,
                ActiveShopId = await runDb
                    .Shops.Where(x => x.Code == "niconi-commons")
                    .Select(x => x.Id)
                    .SingleAsync(TestContext.Current.CancellationToken),
            };
            var characters = new CharacterRepository(
                runDb,
                NullLogger<CharacterRepository>.Instance
            );

            var commonsPayload = new PacketWriter();
            commonsPayload.Write(32_001_001u);
            commonsPayload.Write((byte)0);
            await new AreaNiconiCommonsShopBuyCommonsHandler(
                runDb,
                characters,
                NullLogger<AreaNiconiCommonsShopBuyCommonsHandler>.Instance,
                new DramaCatalog(runDb, TestTextLocaliser.English)
            ).HandleAsync(commonsPayload.ToBytes(), session, TestContext.Current.CancellationToken);

            Assert.Equal(
                0u,
                new PacketReader(
                    session
                        .Sent.Single(p => p.Type == PacketType.NiconiCommonsShopBuyResponse)
                        .Payload
                ).ReadUInt()
            );
            Assert.Equal(
                32_001_001u,
                new PacketReader(
                    session.Sent.Single(p => p.Type == PacketType.NiconiCommonsObtainNotify).Payload
                ).ReadUInt()
            );

            session.Sent.Clear();
            var voicePayload = new PacketWriter();
            voicePayload.Write(10_001u);
            await new AreaNiconiCommonsShopBuyVoiceHandler(
                runDb,
                characters,
                NullLogger<AreaNiconiCommonsShopBuyVoiceHandler>.Instance,
                new DramaCatalog(runDb, TestTextLocaliser.English)
            ).HandleAsync(voicePayload.ToBytes(), session, TestContext.Current.CancellationToken);

            Assert.Equal(
                0u,
                new PacketReader(
                    session
                        .Sent.Single(p => p.Type == PacketType.NiconiCommonsShopBuyResponse)
                        .Payload
                ).ReadUInt()
            );
            Assert.Equal(
                10_001u,
                new PacketReader(
                    session.Sent.Single(p => p.Type == PacketType.UccVoiceObtainNotify).Payload
                ).ReadUInt()
            );

            await using var verify = new MainContext(options);
            Assert.Equal(
                14300002,
                (
                    await verify.Items.SingleAsync(
                        item => item.Id == 172001001,
                        TestContext.Current.CancellationToken
                    )
                ).IconId
            );
            Assert.Equal(
                14300001,
                (
                    await verify.Items.SingleAsync(
                        item => item.Id == 143010001,
                        TestContext.Current.CancellationToken
                    )
                ).IconId
            );

            Assert.Equal(
                11400,
                (
                    await verify.Users.SingleAsync(
                        u => u.Id == userId,
                        TestContext.Current.CancellationToken
                    )
                ).AiPoints
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task End_sends_the_reply_and_the_ended_notify()
    {
        var session = new CapturingPlayerSession { CharacterId = 1 };
        await new AreaNiconiCommonsShopEndHandler().HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(
            [PacketType.NiconiCommonsShopEndResponse, PacketType.NiconiCommonsShopEndedNotify],
            session.Sent.Select(p => p.Type).ToArray()
        );
    }
}
