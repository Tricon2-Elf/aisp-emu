using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Common.Handlers.Area;
using aisp.Common.Localisation;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class QuestSystemTests
{
    [Fact]
    public async Task Seed_InsertsWelcomeQuest()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            await QuestRepository.EnsureSeedQuestsPresentAsync(
                db,
                SeedPath(),
                TestContext.Current.CancellationToken
            );
            var quest = await db.Quests.SingleAsync(
                q => q.Id == 1,
                TestContext.Current.CancellationToken
            );
            Assert.Equal("シンジュに話しかけよう", quest.Title);
            Assert.True(quest.AutoStartOnConnect);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetWork_StartsWelcomeQuestWhenConnectHookWasMissed()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var seedDb = new MainContext(options);
            await QuestRepository.EnsureSeedQuestsPresentAsync(
                seedDb,
                SeedPath(),
                TestContext.Current.CancellationToken
            );
            await TestDb.SeedCharacterAsync(options, 44, TestContext.Current.CancellationToken);

            await using var db = new MainContext(options);
            var session = await CreateSessionAsync(db, 44, firstLoad: false);
            var handler = new AreaQuestWorkGetHandler(
                new QuestService(new QuestRepository(db), TestTextLocaliser.English)
            );

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var work = QuestGetWorkResponse.FromBytes(
                session.Sent.Single(p => p.Type == PacketType.QuestGetWorkResponse).Payload
            );
            Assert.Equal(1u, Assert.Single(work.Quests).Base.QuestId);
            Assert.True(
                await db.CharacterQuestWorks.AnyAsync(
                    x => x.CharacterId == 44 && x.QuestId == 1,
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
    public async Task MapDataEnterEnd_StartsWelcomeQuestAndPushesTracker()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var seedDb = new MainContext(options);
            await QuestRepository.EnsureSeedQuestsPresentAsync(
                seedDb,
                SeedPath(),
                TestContext.Current.CancellationToken
            );
            await TestDb.SeedCharacterAsync(options, 41, TestContext.Current.CancellationToken);

            await using var db = new MainContext(options);
            var session = await CreateSessionAsync(db, 41, firstLoad: true);
            var handler = new AreaMapDataEnterEndHandler(
                NullLogger<AreaMapDataEnterEndHandler>.Instance,
                quests: new QuestService(new QuestRepository(db), TestTextLocaliser.English)
            );

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Contains(session.Sent, p => p.Type == PacketType.QuestStartedNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.QuestSetTargetNotify);

            var started = QuestStartedNotify.FromBytes(
                session.Sent.Single(p => p.Type == PacketType.QuestStartedNotify).Payload
            );
            Assert.Equal(1u, started.Quest.Base.QuestId);
            Assert.Equal("Talk to Shinju", started.Quest.Base.Title);
            Assert.Equal("Shinju", started.Quest.Base.ShortName);
            Assert.Equal("Sotokanda Building", started.Quest.LocationName);
            Assert.Equal("Talk to Shinju", started.Quest.TargetName);
            Assert.Equal((ushort)1, started.Quest.Required);

            var target = QuestSetTargetNotify.FromBytes(
                session.Sent.Single(p => p.Type == PacketType.QuestSetTargetNotify).Payload
            );
            Assert.Equal(1u, target.QuestId);
            Assert.Equal("Talk to Shinju", target.TargetName);

            Assert.True(
                await db.CharacterQuestWorks.AnyAsync(
                    x => x.CharacterId == 41 && x.QuestId == 1,
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
    public async Task GetWorkAndHistory_ReflectActiveThenCompletedQuest()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var seedDb = new MainContext(options);
            await QuestRepository.EnsureSeedQuestsPresentAsync(
                seedDb,
                SeedPath(),
                TestContext.Current.CancellationToken
            );
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);

            await using var db = new MainContext(options);
            var session = await CreateSessionAsync(db, 42, firstLoad: true);
            var quests = new QuestService(new QuestRepository(db), TestTextLocaliser.English);
            await quests.OnPlayerConnectedAsync(session, TestContext.Current.CancellationToken);

            var workHandler = new AreaQuestWorkGetHandler(quests);
            await workHandler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );
            var work = QuestGetWorkResponse.FromBytes(
                session.Sent.Single(p => p.Type == PacketType.QuestGetWorkResponse).Payload
            );
            Assert.Equal(0u, work.Result);
            Assert.Equal(1u, Assert.Single(work.Quests).Base.QuestId);

            var historyHandler = new AreaQuestHistoryGetHandler(quests);
            await historyHandler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );
            var emptyHistory = QuestGetHistoryResponse.FromBytes(
                session.Sent.Single(p => p.Type == PacketType.QuestGetHistoryResponse).Payload
            );
            Assert.Empty(emptyHistory.History);

            await quests.CompleteAsync(session, 1, ct: TestContext.Current.CancellationToken);
            Assert.Contains(session.Sent, p => p.Type == PacketType.QuestEndedNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.QuestAddHistoryNotify);

            session.Sent.Clear();
            await workHandler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Empty(
                QuestGetWorkResponse
                    .FromBytes(
                        session.Sent.Single(p => p.Type == PacketType.QuestGetWorkResponse).Payload
                    )
                    .Quests
            );

            await historyHandler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );
            var history = QuestGetHistoryResponse.FromBytes(
                session.Sent.Single(p => p.Type == PacketType.QuestGetHistoryResponse).Payload
            );
            var entry = Assert.Single(history.History);
            Assert.Equal(1u, entry.Base.QuestId);
            Assert.Equal((byte)1, entry.Result);
            Assert.Equal(0u, entry.RelatedQuestId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Connect_DoesNotDuplicateWelcomeQuest()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var seedDb = new MainContext(options);
            await QuestRepository.EnsureSeedQuestsPresentAsync(
                seedDb,
                SeedPath(),
                TestContext.Current.CancellationToken
            );
            await TestDb.SeedCharacterAsync(options, 43, TestContext.Current.CancellationToken);

            await using var db = new MainContext(options);
            var quests = new QuestService(new QuestRepository(db), TestTextLocaliser.English);
            var first = await CreateSessionAsync(db, 43, firstLoad: true);
            await quests.OnPlayerConnectedAsync(first, TestContext.Current.CancellationToken);
            var second = await CreateSessionAsync(db, 43, firstLoad: true);
            await quests.OnPlayerConnectedAsync(second, TestContext.Current.CancellationToken);

            Assert.Equal(
                1,
                await db.CharacterQuestWorks.CountAsync(
                    x => x.CharacterId == 43,
                    TestContext.Current.CancellationToken
                )
            );
            Assert.Single(second.Sent, p => p.Type == PacketType.QuestStartedNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task TalkingToShinju_CompletesTalkQuest()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var seedDb = new MainContext(options);
            await QuestRepository.EnsureSeedQuestsPresentAsync(
                seedDb,
                SeedPath(),
                TestContext.Current.CancellationToken
            );
            await TestDb.SeedCharacterAsync(options, 45, TestContext.Current.CancellationToken);

            await using var db = new MainContext(options);
            var quests = new QuestService(new QuestRepository(db), TestTextLocaliser.English);
            var session = await CreateSessionAsync(db, 45, firstLoad: true);
            await quests.OnPlayerConnectedAsync(session, TestContext.Current.CancellationToken);
            session.Sent.Clear();

            var eventRepository = new CharacterEventRepository(db);
            var serverScriptSession = new ServerScriptSession(
                eventRepository,
                NullLogger<ServerScriptSession>.Instance
            );
            var dispatcher = new ServerScriptDispatcher(
                [
                    new ShinjuRegistrationServerScript(
                        new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                        eventRepository,
                        new MapRepository(db),
                        serverScriptSession,
                        TestTextLocaliser.English,
                        NullLogger<ShinjuRegistrationServerScript>.Instance,
                        quests
                    ),
                ],
                serverScriptSession,
                NullLogger<ServerScriptDispatcher>.Instance
            );
            var context = new ServerScriptContext
            {
                Npc = new Npc
                {
                    NpcObjectId = 1342177291,
                    Name = "Shinju",
                    EventKey = ServerEvents.Keys.ShinjuRegistration,
                },
            };

            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.ShinjuRegistration,
                context,
                EventCompletionPolicy.Once,
                TestContext.Current.CancellationToken
            );

            Assert.Contains(session.Sent, p => p.Type == PacketType.QuestEndedNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.QuestAddHistoryNotify);
            Assert.False(
                await db.CharacterQuestWorks.AnyAsync(
                    x => x.CharacterId == 45 && x.QuestId == QuestIds.TalkToShinju,
                    TestContext.Current.CancellationToken
                )
            );
            Assert.True(
                await db.CharacterQuestHistories.AnyAsync(
                    x => x.CharacterId == 45 && x.QuestId == QuestIds.TalkToShinju,
                    TestContext.Current.CancellationToken
                )
            );

            session.Sent.Clear();
            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.ShinjuRegistration,
                context,
                EventCompletionPolicy.Once,
                TestContext.Current.CancellationToken
            );
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.QuestEndedNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public void CatalogSeeder_CollectsWelcomeQuestLocales()
    {
        var rows = LocalisationCatalogSeeder.CollectFromDirectory(
            Path.GetDirectoryName(SeedPath())!
        );
        Assert.Contains(
            rows,
            row =>
                row.Key == L.Quest.Title(1).Value
                && row.Language == GameLanguage.English
                && row.Value == "Talk to Shinju"
        );
    }

    private static async Task<CapturingPlayerSession> CreateSessionAsync(
        MainContext db,
        int characterId,
        bool firstLoad
    )
    {
        var character = await db
            .Characters.Include(c => c.User)
            .SingleAsync(c => c.Id == characterId, TestContext.Current.CancellationToken);
        return new CapturingPlayerSession
        {
            UserId = character.UserId,
            CharacterId = (uint)character.Id,
            User = character.User,
            Language = GameLanguage.English,
            NeedsPostLoadSelfAvatarNotify = firstLoad,
        };
    }

    private static string SeedPath()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "seedData");
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);
        return Path.Combine(directory, "quests.json");
    }
}
