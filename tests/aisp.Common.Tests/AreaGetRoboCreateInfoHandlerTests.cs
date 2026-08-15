using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Tests;

public class AreaGetRoboCreateInfoHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsDefaultModelHairstyleAndEquips()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var handler = new AreaGetRoboCreateInfoHandler(db);
            var session = new CapturingPlayerSession { CharacterId = 1 };

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var sent = Assert.Single(session.Sent);
            Assert.Equal(PacketType.GetRoboCreateInfoResponse, sent.Type);

            var reader = new PacketReader(sent.Payload);
            Assert.Equal(1002011u, reader.ReadUInt());
            Assert.Equal(10930010u, reader.ReadUInt());
            Assert.Equal(4u, reader.ReadUInt());
            Assert.Equal(10100060u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(10200090u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(10400000u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(10500010u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(44, sent.Payload.Length);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Theory]
    [InlineData(1u, CharadollPersonality.Quiet, 2012020u)]
    [InlineData(1u, CharadollPersonality.Active, 2012030u)]
    [InlineData(2u, CharadollPersonality.Quiet, 2022030u)]
    [InlineData(2u, CharadollPersonality.Active, 2022020u)]
    [InlineData(3u, CharadollPersonality.Quiet, 2032020u)]
    [InlineData(3u, CharadollPersonality.Active, 2032030u)]
    public async Task HandleAsync_ReturnsMatchingCharadollModelWithBuiltinHair(
        uint homeIslandId,
        CharadollPersonality personality,
        uint expectedModelId
    )
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            db.Users.Add(new User { Id = 1, Username = "tester" });
            db.Characters.Add(
                new Character
                {
                    Id = 10,
                    UserId = 1,
                    Name = "Doll",
                    HomeIslandId = homeIslandId,
                    CharadollPersonality = personality,
                    ModelId = 1002011,
                    Hairstyle = 10930010,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var handler = new AreaGetRoboCreateInfoHandler(db);
            var session = new CapturingPlayerSession { CharacterId = 10 };

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var sent = Assert.Single(session.Sent);
            var reader = new PacketReader(sent.Payload);
            Assert.Equal(expectedModelId, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public void GetRoboCreateInfoResponse_MatchesClientMaxEquipBuffer()
    {
        // Client alloc is 252 bytes = 3×uint + 30×(itemId+socket).
        var equips = Enumerable
            .Range(0, 30)
            .Select(i => new aisp.Network.Data.ItemSlotInfo((uint)(1000 + i), 0))
            .ToList();
        var bytes = new GetRoboCreateInfoResponse(1, 2, equips).ToBytes();
        Assert.Equal(252, bytes.Length);
    }
}
