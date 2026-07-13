using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Services;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class ScriptedEventTests
{
    [Fact]
    public void IsWithinRadius_ReturnsTrue_WhenInsideMarker()
    {
        var trigger = ScriptedEventTriggers.OnMovement[0];
        var sample = new MovementPositionSample(-9200f, 2f, -16887f);

        Assert.True(ScriptedEventTriggers.IsWithinRadius(sample, trigger));
    }

    [Fact]
    public void IsWithinRadius_ReturnsFalse_WhenOutsideMarker()
    {
        var trigger = ScriptedEventTriggers.OnMovement[0];
        var sample = new MovementPositionSample(0f, 2f, 0f);

        Assert.False(ScriptedEventTriggers.IsWithinRadius(sample, trigger));
    }

    [Fact]
    public async Task TriggerService_StartsIntroduction_WhenMovementEntersMarker()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await SeedCharacterAsync(options, 9001);
            await using var db = new MainContext(options);
            var service = CreateTriggerService(db);
            var session = new CapturingPlayerSession
            {
                CharacterId = 9001,
                UserId = 1,
                MapId = ScriptedEventTriggers.AkihabaraMapId,
                ChannelId = 1,
            };

            var started = await service.TryStartOnMovementAsync(session, [new MovementPositionSample(-9200f, 2f, -16887f)], TestContext.Current.CancellationToken);

            Assert.True(started);
            Assert.Equal(ScriptedEvents.Keys.IntroductionRin01, session.ActiveEventKey);
            Assert.Equal(NpcEventKind.ClientScript, session.ActiveEventKind);
            Assert.Equal(EventCompletionPolicy.Once, session.ActiveEventCompletionPolicy);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventStartNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventScriptPlayNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task TriggerService_SkipsIntroduction_WhenAlreadyCompleted()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await SeedCharacterAsync(options, 9002);
            await using var db = new MainContext(options);
            var eventRepo = new CharacterEventRepository(db);
            await eventRepo.MarkCompletedAsync(9002, ScriptedEvents.Keys.IntroductionRin01, TestContext.Current.CancellationToken);

            var service = CreateTriggerService(eventRepo);
            var session = new CapturingPlayerSession
            {
                CharacterId = 9002,
                UserId = 1,
                MapId = ScriptedEventTriggers.AkihabaraMapId,
                ChannelId = 1,
            };

            var started = await service.TryStartOnMovementAsync(session, [new MovementPositionSample(-9200f, 2f, -16887f)], TestContext.Current.CancellationToken);

            Assert.False(started);
            Assert.Null(session.ActiveEventKey);
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.EventStartNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task TriggerService_SkipsIntroduction_WhenOutsideMarker()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await SeedCharacterAsync(options, 9003);
            await using var db = new MainContext(options);
            var service = CreateTriggerService(db);
            var session = new CapturingPlayerSession
            {
                CharacterId = 9003,
                UserId = 1,
                MapId = ScriptedEventTriggers.AkihabaraMapId,
                ChannelId = 1,
            };

            var started = await service.TryStartOnMovementAsync(session, [new MovementPositionSample(0f, 2f, 0f)], TestContext.Current.CancellationToken);

            Assert.False(started);
            Assert.Null(session.ActiveEventKey);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventFadeInHandler_MarksScriptedEventComplete_WhenActive()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await SeedCharacterAsync(options, 42);
            await using var db = new MainContext(options);
            var eventRepo = new CharacterEventRepository(db);
            var areaSession = new CapturingPlayerSession
            {
                CharacterId = 42,
                UserId = 1,
                PendingEventEndAfterFade = true,
                ActiveEventKey = ScriptedEvents.Keys.IntroductionRin01,
                ActiveEventKind = NpcEventKind.ClientScript,
                ActiveEventCompletionPolicy = EventCompletionPolicy.Once,
            };

            var handler = new AreaEventFadeInHandler(eventRepo, NullLogger<AreaEventFadeInHandler>.Instance);
            await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, areaSession, TestContext.Current.CancellationToken);

            Assert.Null(areaSession.ActiveEventKey);
            Assert.True(await eventRepo.HasCompletedAsync(42, ScriptedEvents.Keys.IntroductionRin01, TestContext.Current.CancellationToken));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventFadeInHandler_DoesNotMarkComplete_WhenReplayable()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await SeedCharacterAsync(options, 43);
            await using var db = new MainContext(options);
            var eventRepo = new CharacterEventRepository(db);
            var areaSession = new CapturingPlayerSession
            {
                CharacterId = 43,
                UserId = 1,
                PendingEventEndAfterFade = true,
                ActiveEventKey = ScriptedEvents.Keys.IntroductionRin02,
                ActiveEventKind = NpcEventKind.ClientScript,
                ActiveEventCompletionPolicy = EventCompletionPolicy.Replayable,
            };

            var handler = new AreaEventFadeInHandler(eventRepo, NullLogger<AreaEventFadeInHandler>.Instance);
            await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, areaSession, TestContext.Current.CancellationToken);

            Assert.Null(areaSession.ActiveEventKey);
            Assert.False(await eventRepo.HasCompletedAsync(43, ScriptedEvents.Keys.IntroductionRin02, TestContext.Current.CancellationToken));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventScriptPlayHandler_Failure_ClearsActiveEventKey()
    {
        var areaSession = new CapturingPlayerSession
        {
            CharacterId = 1,
            UserId = 2,
            ActiveEventKey = ScriptedEvents.Keys.IntroductionRin01,
            ActiveEventKind = NpcEventKind.ClientScript,
        };
        var handler = new AreaEventScriptPlayHandler(NullLogger<AreaEventScriptPlayHandler>.Instance);

        var writer = new PacketWriter();
        writer.Write(1u);
        await handler.HandleAsync(writer.ToBytes(), areaSession, TestContext.Current.CancellationToken);

        Assert.Null(areaSession.ActiveEventKey);
    }

    [Fact]
    public async Task CharacterEventRepository_MarkCompleted_IsIdempotent()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await SeedCharacterAsync(options, 77);
            await using var db = new MainContext(options);
            var repo = new CharacterEventRepository(db);

            await repo.MarkCompletedAsync(77, ScriptedEvents.Keys.IntroductionRin01, TestContext.Current.CancellationToken);
            await repo.MarkCompletedAsync(77, ScriptedEvents.Keys.IntroductionRin01, TestContext.Current.CancellationToken);

            await using var verify = new MainContext(options);
            Assert.Equal(1, await verify.CharacterEventStatuses.CountAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static ScriptedEventTriggerService CreateTriggerService(MainContext db) => CreateTriggerService(new CharacterEventRepository(db));

    private static ScriptedEventTriggerService CreateTriggerService(CharacterEventRepository eventRepo) => new(eventRepo, NullLogger<ScriptedEventTriggerService>.Instance);

    private static async Task SeedCharacterAsync(DbContextOptions<MainContext> options, int characterId)
    {
        await using var db = new MainContext(options);
        var user = new User { Id = 1, Username = $"user-{characterId}" };
        user.SetPassword("pw");
        db.Users.Add(user);
        db.Characters.Add(
            new Character
            {
                Id = characterId,
                Name = $"Character {characterId}",
                UserId = user.Id,
                CurrentMapId = ScriptedEventTriggers.AkihabaraMapId,
            }
        );
        await db.SaveChangesAsync();
    }
}
