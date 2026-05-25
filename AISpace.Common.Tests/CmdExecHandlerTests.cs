using AISpace.Common.Config;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Handlers.Msg;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Tests;

public class CmdExecHandlerTests
{
    [Fact]
    public async Task TeleCommand_MovesAreaSessionToRequestedMap()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 8001, "tele-user", "Tele User", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Channels.Add(
                    new GameChannel
                    {
                        ChannelNum = 1,
                        IP = "localhost",
                        Port = 50054,
                        MapId = 10990100,
                    }
                );
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Akihabara",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 90,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Akihabara 2",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.First(),
                CharacterId = 8001,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
            };

            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(BuildCmdExecPayload("tele", "10990110"), msgSession, TestContext.Current.CancellationToken);

            Assert.Equal(10990110u, areaSession.MapId);
            Assert.Equal(-11000f, areaSession.X);
            Assert.Equal(0.1f, areaSession.Y);
            Assert.Equal(-19200f, areaSession.Z);
            Assert.Equal(10990110u, areaSession.Character!.CurrentMapId);
            Assert.Contains(areaSession.Sent, packet => packet.Type == PacketType.NotifyChangeMap);
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);

            await using var verifyDb = new MainContext(options);
            var persisted = await verifyDb.Characters.SingleAsync(c => c.Id == 8001, TestContext.Current.CancellationToken);
            Assert.Equal(10990110u, persisted.CurrentMapId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PosCommand_ResolvesLiveAreaSession_NotPersistedPresenceSnapshot()
    {
        var user = CreateUserWithCharacter(1, 8002, "pos-user", "Pos User", 10990100);
        var state = new SharedState();
        var areaSession = new CapturingPlayerSession
        {
            User = user,
            UserId = user.Id,
            CharacterId = 8002,
            X = -9100f,
            Z = -18000f,
        };
        state.RegisterClient(ServerType.Area, areaSession);

        areaSession.X = -9055.5f;
        areaSession.Z = -17988.75f;

        var resolved = state.GetAreaSessionByUserId(user.Id);
        Assert.Same(areaSession, resolved);
        Assert.Equal(-9055.5f, resolved!.X);
        Assert.Equal(-17988.75f, resolved.Z);
    }

    private static byte[] BuildCmdExecPayload(string command, params string[] args)
    {
        var writer = new PacketWriter();
        writer.Write(1u);
        writer.WriteFixedString(command, 96, "ASCII");
        for (var i = 0; i < 10; i++)
            writer.WriteFixedString(i < args.Length ? args[i] : string.Empty, 384, "ASCII");
        writer.Write((uint)args.Length);
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
            }
        );
        return user;
    }

    private static DirectMapLinkTransitionService CreateDirectMapLinkTransitionService(DbContextOptions<MainContext> options, SharedState state)
    {
        return new DirectMapLinkTransitionService(
            new MapRepository(new MainContext(options)),
            new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance),
            new MapLinkRepository(new MainContext(options)),
            new ChannelRepository(new MainContext(options)),
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
    }
}
