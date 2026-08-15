using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AreaRoboCallHandlerTests
{
    [Fact]
    public async Task HandleAsync_ActivatesOwnedRoboInMyRoom()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            var objectId = RoboRepository.GetObjectId(1, 1);
            var chara = new CharaData(objectId, 1002011, "Robot")
            {
                Visual = new CharaVisual(BloodType.A, 1, 1, 0, objectId, 0, 10930010),
            };
            await using (var seedDb = new MainContext(options))
            {
                await new RoboRepository(seedDb).UpsertAsync(
                    1,
                    new RoboData(1, chara, state: 1)
                    {
                        OwnerAvatarId = 1,
                        EmotionId = 17,
                        AvailableStatusPoints = 3,
                    },
                    TestContext.Current.CancellationToken
                );
            }

            await using (var handlerDb = new MainContext(options))
            {
                var handler = new AreaRoboCallHandler(
                    new RoboRepository(handlerDb),
                    NullLogger<AreaRoboCallHandler>.Instance
                );
                var session = new CapturingPlayerSession
                {
                    CharacterId = 1,
                    ChannelId = 3,
                    MapId = 40000001,
                    X = 10,
                    Y = 20,
                    Z = 30,
                    Rotation = 180,
                };
                session.AccompanyingRoboIds.Add(1);
                var writer = new PacketWriter();
                writer.Write(1u);

                await handler.HandleAsync(
                    writer.ToBytes(),
                    session,
                    TestContext.Current.CancellationToken
                );

                Assert.Empty(session.AccompanyingRoboIds);
                Assert.Collection(
                    session.Sent,
                    sent =>
                    {
                        Assert.Equal(PacketType.NotifyUpdateRoboState, sent.Type);
                        var stateReader = new PacketReader(sent.Payload);
                        Assert.Equal(1u, stateReader.ReadUInt());
                        Assert.Equal(objectId, stateReader.ReadUInt());
                        Assert.Equal((uint)RoboState.InMyRoom, stateReader.ReadUInt());
                        var map = CharacterMapData.FromBytes(
                            stateReader.ReadBytes(CharacterMapData.WireSize)
                        );
                        Assert.Equal(3u, map.ChannelId);
                        Assert.Equal(40000001u, map.MapId);
                        Assert.Equal(10f, map.Movement.X);
                        Assert.Equal(20f, map.Movement.Y);
                        Assert.Equal(30f, map.Movement.Z);
                        Assert.Equal(180, map.Movement.Rotation);
                    },
                    sent =>
                    {
                        Assert.Equal(PacketType.RoboCallResponse, sent.Type);
                        var callReader = new PacketReader(sent.Payload);
                        Assert.Equal(1u, callReader.ReadUInt());
                        Assert.Equal(0u, callReader.ReadUInt());
                    }
                );
            }

            await using var restartedDb = new MainContext(options);
            var stored = await new RoboRepository(restartedDb).GetAsync(
                1,
                1,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(stored);
            Assert.Equal((uint)RoboState.InMyRoom, stored.State);
            Assert.Equal(1u, stored.OwnerAvatarId);
            Assert.Equal(0u, stored.EmotionId);
            Assert.Equal(3u, stored.AvailableStatusPoints);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
