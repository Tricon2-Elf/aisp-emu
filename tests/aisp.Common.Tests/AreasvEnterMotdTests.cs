using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Localisation;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace aisp.Common.Tests;

public class AreasvEnterMotdTests
{
    [Fact]
    public async Task SuccessfulEnter_MarksMotdPending_AndDoesNotSendYet()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var user = await SeedAsync(options, GameLanguage.English);
            var session = new CapturingPlayerSession();
            await using var handlerDb = new MainContext(options);
            var handler = CreateEnterHandler(handlerDb, new SharedState());

            await handler.HandleAsync(
                BuildEnterPayload((uint)user.Id, Otp),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Contains(session.Sent, packet => packet.Type == PacketType.AreasvEnterResponse);
            Assert.True(session.NeedsMotd);
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task FailedEnter_DoesNotMarkMotdPending()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await SeedAsync(options, GameLanguage.English);
            var session = new CapturingPlayerSession();
            await using var handlerDb = new MainContext(options);
            var handler = CreateEnterHandler(handlerDb, new SharedState());

            await handler.HandleAsync(
                BuildEnterPayload(1, "unknown-otp-12345678"),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.False(session.NeedsMotd);
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task FirstMapEnter_SendsMotdAsSystemNotice()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var user = await SeedAsync(options, GameLanguage.English);
            var state = new SharedState();
            var session = CreateMapSession(user);
            session.NeedsMotd = true;
            await using var runDb = new MainContext(options);
            var handler = CreateMapEnterHandler(
                runDb,
                state,
                new MotdOptions { Enabled = true, Message = "Welcome to AISP" }
            );

            await handler.HandleAsync(
                BuildMapEnterPayload(10990100, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.False(session.NeedsMotd);
            AssertSystemNotice(session, "Welcome to AISP");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task FirstMapEnter_SendsLanguageSpecificMotd()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var user = await SeedAsync(options, GameLanguage.Japanese);
            var session = CreateMapSession(user);
            session.NeedsMotd = true;
            session.Language = GameLanguage.Japanese;
            await using var runDb = new MainContext(options);
            var handler = CreateMapEnterHandler(
                runDb,
                new SharedState(),
                new MotdOptions
                {
                    Enabled = true,
                    Message = "Welcome",
                    Messages = { ["ja"] = "ようこそ" },
                }
            );

            await handler.HandleAsync(
                BuildMapEnterPayload(10990100, 1),
                session,
                TestContext.Current.CancellationToken
            );

            AssertSystemNotice(session, "ようこそ");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task FirstMapEnter_SendsMotdOnMsgSessionWhenPresent()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var user = await SeedAsync(options, GameLanguage.English);
            var state = new SharedState();
            var area = CreateMapSession(user);
            area.NeedsMotd = true;
            var msg = new CapturingPlayerSession { User = user, UserId = user.Id };
            state.RegisterClient(ServerType.Msg, msg);
            await using var runDb = new MainContext(options);
            var handler = CreateMapEnterHandler(
                runDb,
                state,
                new MotdOptions { Enabled = true, Message = "Hello" }
            );

            await handler.HandleAsync(
                BuildMapEnterPayload(10990100, 1),
                area,
                TestContext.Current.CancellationToken
            );

            Assert.DoesNotContain(area.Sent, packet => packet.Type == PacketType.TalkForwardNotify);
            AssertSystemNotice(msg, "Hello");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task LaterMapEnter_DoesNotSendMotdAgain()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var user = await SeedAsync(options, GameLanguage.English);
            var state = new SharedState();
            var session = CreateMapSession(user);
            session.NeedsMotd = true;
            await using var runDb = new MainContext(options);
            var handler = CreateMapEnterHandler(
                runDb,
                state,
                new MotdOptions { Enabled = true, Message = "Welcome" }
            );

            await handler.HandleAsync(
                BuildMapEnterPayload(10990100, 1),
                session,
                TestContext.Current.CancellationToken
            );
            session.Sent.Clear();

            await handler.HandleAsync(
                BuildMapEnterPayload(10990100, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.False(session.NeedsMotd);
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task DisabledOrEmptyMotd_IsNotSentOnFirstMapEnter()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var user = await SeedAsync(options, GameLanguage.English);
            await using var runDb = new MainContext(options);

            var disabled = CreateMapSession(user);
            disabled.NeedsMotd = true;
            await CreateMapEnterHandler(
                    runDb,
                    new SharedState(),
                    new MotdOptions { Enabled = false, Message = "hidden" }
                )
                .HandleAsync(
                    BuildMapEnterPayload(10990100, 1),
                    disabled,
                    TestContext.Current.CancellationToken
                );
            Assert.False(disabled.NeedsMotd);
            Assert.DoesNotContain(
                disabled.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );

            var empty = CreateMapSession(user);
            empty.NeedsMotd = true;
            await CreateMapEnterHandler(
                    runDb,
                    new SharedState(),
                    new MotdOptions { Enabled = true, Message = "" }
                )
                .HandleAsync(
                    BuildMapEnterPayload(10990100, 1),
                    empty,
                    TestContext.Current.CancellationToken
                );
            Assert.False(empty.NeedsMotd);
            Assert.DoesNotContain(
                empty.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private const string Otp = "motd-enter-otp-12";

    private static AreasvEnterHandler CreateEnterHandler(
        MainContext handlerDb,
        SharedState state
    ) =>
        new(
            new UserSessionRepository(handlerDb, NullLogger<UserSessionRepository>.Instance),
            new UserRepository(handlerDb),
            new MapRepository(handlerDb),
            new ChannelRepository(handlerDb),
            new CharacterRepository(handlerDb, NullLogger<CharacterRepository>.Instance),
            new MyRoomRepository(handlerDb),
            new CircleRepository(handlerDb),
            new FriendRepository(handlerDb),
            state,
            NullLogger<AreasvEnterHandler>.Instance
        );

    private static AreaMapEnterHandler CreateMapEnterHandler(
        MainContext db,
        SharedState state,
        MotdOptions motd
    ) =>
        new(
            new MapRepository(db),
            new DirectMapLinkTransitionService(
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
                TestTextLocaliser.English,
                NullLogger<DirectMapLinkTransitionService>.Instance
            ),
            state,
            NullLogger<AreaMapEnterHandler>.Instance,
            motdOptions: Options.Create(motd)
        );

    private static CapturingPlayerSession CreateMapSession(User user)
    {
        var character = user.Characters.First();
        return new CapturingPlayerSession
        {
            User = user,
            UserId = user.Id,
            Character = character,
            CharacterId = (uint)character.Id,
            Language = user.Language,
            MapId = 10990100,
            ChannelId = 1,
            HasMovedSinceMapLoad = false,
        };
    }

    private static async Task<User> SeedAsync(
        DbContextOptions<MainContext> options,
        GameLanguage language
    )
    {
        var user = new User
        {
            Id = 1,
            Username = "motd-user",
            Language = language,
        };
        user.SetPassword("pw");
        user.Characters.Add(
            new Character
            {
                Id = 11,
                Name = "Motd Avatar",
                UserId = 1,
                CurrentMapId = 10990100,
                ModelId = 100,
                Birthdate = new DateTime(2000, 1, 2),
                BloodType = BloodType.A,
                Gender = 1,
                FaceType = 1,
                Hairstyle = 2,
            }
        );

        await using var db = new MainContext(options);
        db.Users.Add(user);
        db.UserSessions.Add(
            new UserSession
            {
                UserId = user.Id,
                OTP = Otp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            }
        );
        db.Channels.Add(
            new GameChannel
            {
                ChannelNum = 1,
                IP = "localhost",
                Port = 50054,
                CurrentUsers = 0,
                MaxUsers = 1000,
                MapId = 10990100,
            }
        );
        db.Maps.Add(
            new Map
            {
                MapId = 10990100,
                Name = "Akihabara",
                SpawnX = -9100f,
                SpawnY = 2f,
                SpawnZ = -18000f,
                SpawnRotation = 180,
            }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    private static void AssertSystemNotice(CapturingPlayerSession session, string expected)
    {
        var notice = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.TalkForwardNotify
        );
        var reader = new PacketReader(notice.Payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(SystemNotice.DistId, reader.ReadUInt());
        Assert.Equal($"{expected}\r\n", reader.ReadString("utf-8"));
    }

    private static byte[] BuildEnterPayload(uint userId, string otp)
    {
        var writer = new PacketWriter();
        writer.Write(userId);
        writer.WriteFixedString(otp, 20, "ASCII");
        return writer.ToBytes();
    }

    private static byte[] BuildMapEnterPayload(uint mapId, uint channelId)
    {
        var writer = new PacketWriter();
        writer.Write(mapId);
        writer.Write(channelId);
        return writer.ToBytes();
    }
}
