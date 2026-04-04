using AISpace.Common.Config;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Handlers.Msg;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Tests;

public class MsgHandlersTests
{
    [Fact]
    public async Task GetChannelListMapHandler_ReturnsExactMapChannels_WhenPresent()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await using (var db = new MainContext(options))
            {
                db.Channels.AddRange(
                    new GameChannel
                    {
                        ChannelNum = 1,
                        IP = "localhost",
                        Port = 50054,
                        CurrentUsers = 3,
                        MaxUsers = 1000,
                        MapId = 10990100,
                    },
                    new GameChannel
                    {
                        ChannelNum = 2,
                        IP = "localhost",
                        Port = 50055,
                        CurrentUsers = 4,
                        MaxUsers = 1000,
                        MapId = 10990200,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var handler = CreateHandler(options);
            var session = new CapturingPlayerSession();

            await handler.HandleAsync(BuildUIntPayload(10990200), session, TestContext.Current.CancellationToken);

            Assert.Single(session.Sent);
            Assert.Equal(PacketType.GetChannelListMapResponse, session.Sent[0].Type);

            var reader = new PacketReader(session.Sent[0].Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(2u, reader.ReadUInt());
            Assert.Equal(4f, reader.ReadFloat());
            Assert.Equal(1000u, reader.ReadUInt());
            Assert.Equal((ushort)50055, reader.ReadUShort());
            Assert.Equal("localhost", reader.ReadFixedString(65, "ASCII"));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetChannelListMapHandler_FallsBackToMapGroupChannels_WhenExactMapMissing()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await using (var db = new MainContext(options))
            {
                db.Channels.AddRange(
                    new GameChannel
                    {
                        ChannelNum = 1,
                        IP = "localhost",
                        Port = 50054,
                        CurrentUsers = 0,
                        MaxUsers = 1000,
                        MapId = 10990100,
                    },
                    new GameChannel
                    {
                        ChannelNum = 2,
                        IP = "localhost",
                        Port = 50055,
                        CurrentUsers = 0,
                        MaxUsers = 1000,
                        MapId = 10010100,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var handler = CreateHandler(options);
            var session = new CapturingPlayerSession();

            await handler.HandleAsync(BuildUIntPayload(10990200), session, TestContext.Current.CancellationToken);

            Assert.Single(session.Sent);
            Assert.Equal(PacketType.GetChannelListMapResponse, session.Sent[0].Type);

            var reader = new PacketReader(session.Sent[0].Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(0f, reader.ReadFloat());
            Assert.Equal(1000u, reader.ReadUInt());
            Assert.Equal((ushort)50054, reader.ReadUShort());
            Assert.Equal("localhost", reader.ReadFixedString(65, "ASCII"));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetChannelListMapHandler_FallbackPrefersCurrentSessionChannel_WhenMultipleGroupMatchesExist()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await using (var db = new MainContext(options))
            {
                db.Channels.AddRange(
                    new GameChannel
                    {
                        ChannelNum = 1,
                        IP = "localhost",
                        Port = 50054,
                        CurrentUsers = 0,
                        MaxUsers = 1000,
                        MapId = 10990100,
                    },
                    new GameChannel
                    {
                        ChannelNum = 2,
                        IP = "localhost",
                        Port = 50055,
                        CurrentUsers = 0,
                        MaxUsers = 1000,
                        MapId = 10990110,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var handler = CreateHandler(options);
            var session = new CapturingPlayerSession { ChannelId = 1 };

            await handler.HandleAsync(BuildUIntPayload(10990200), session, TestContext.Current.CancellationToken);

            Assert.Single(session.Sent);
            Assert.Equal(PacketType.GetChannelListMapResponse, session.Sent[0].Type);

            var reader = new PacketReader(session.Sent[0].Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(0f, reader.ReadFloat());
            Assert.Equal(1000u, reader.ReadUInt());
            Assert.Equal((ushort)50054, reader.ReadUShort());
            Assert.Equal("localhost", reader.ReadFixedString(65, "ASCII"));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ChannelSelectHandler_StoresSelectedChannelAndMapOnSession()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await using (var db = new MainContext(options))
            {
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
                db.Users.Add(CreateUserWithCharacter(1, 5001, "msg-user", "Msg User", 10990100));
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var services = new ServiceCollection();
            services.AddDbContext<MainContext>(builder => builder.UseSqlite(connection));
            var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
            var handler = new ChannelSelectHandler(
                NullLogger<ChannelSelectHandler>.Instance,
                scopeFactory,
                Options.Create(
                    new ServerOptions
                    {
                        NetworkOptions = new NetworkOptions(),
                        DbOptions = new DbOptions(),
                        IPOverride = "localhost",
                    }
                ),
                new ChannelRepository(new MainContext(options))
            );
            var session = new CapturingPlayerSession { User = CreateUserWithCharacter(1, 5001, "msg-user", "Msg User", 10990100), UserId = 1 };

            await handler.HandleAsync(BuildUIntPayload(1), session, TestContext.Current.CancellationToken);

            Assert.Equal(1, session.ChannelId);
            Assert.Equal(10990100u, session.MapId);
            Assert.Collection(session.Sent, packet => Assert.Equal(PacketType.ChannelSelectResponse, packet.Type));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static GetChannelListMapHandler CreateHandler(DbContextOptions<MainContext> options)
    {
        return new GetChannelListMapHandler(
            Options.Create(
                new ServerOptions
                {
                    NetworkOptions = new NetworkOptions(),
                    DbOptions = new DbOptions(),
                    IPOverride = "localhost",
                }
            ),
            new ChannelRepository(new MainContext(options)),
            NullLogger<GetChannelListMapHandler>.Instance
        );
    }

    private static byte[] BuildUIntPayload(uint value)
    {
        var writer = new PacketWriter();
        writer.Write(value);
        return writer.ToBytes();
    }

    private static User CreateUserWithCharacter(int userId, int characterId, string username, string characterName, uint currentMapId)
    {
        var user = new User { Id = userId, Username = username };
        user.SetPassword("pw");
        user.Characters.Add(
            new Character
            {
                Id = characterId,
                Name = characterName,
                UserId = userId,
                CurrentMapId = currentMapId,
                ModelId = 100,
                Birthdate = new DateTime(2000, 1, 2),
                BloodType = BloodType.A,
                Gender = 1,
                FaceType = 1,
                Hairstyle = 2,
                Like1 = "Like 1",
                Like2 = "Like 2",
                Like3 = "Like 3",
                LikeDesc1 = "Desc 1",
                LikeDesc2 = "Desc 2",
                LikeDesc3 = "Desc 3",
                AvatarDesc = "Hello there",
            }
        );
        return user;
    }
}
