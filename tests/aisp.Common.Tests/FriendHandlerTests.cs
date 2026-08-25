using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public sealed class FriendHandlerTests
{
    [Fact]
    public async Task RequestAndAccept_PersistsFriendshipAndPopulatesList()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 1, ct);
        await TestDb.SeedCharacterAsync(options, 2, ct);

        await using var db = new MainContext(options);
        var friends = new FriendRepository(db);
        var state = new SharedState();
        var requester = new CapturingPlayerSession
        {
            CharacterId = 1,
            Character = await db.Characters.SingleAsync(x => x.Id == 1, ct),
            User = await db.Users.SingleAsync(x => x.Id == 1, ct),
        };
        var target = new CapturingPlayerSession
        {
            CharacterId = 2,
            Character = await db.Characters.SingleAsync(x => x.Id == 2, ct),
            User = await db.Users.SingleAsync(x => x.Id == 2, ct),
        };
        state.RegisterClient(ServerType.Area, requester);
        state.RegisterClient(ServerType.Area, target);

        var requestHandler = new AreaRequestAddFriendListHandler(friends, state);
        var response = await requestHandler.HandleAsync(
            new RequestAddFriendListRequest(2),
            requester,
            ct
        );
        Assert.NotNull(response);
        Assert.Equal(0u, new PacketReader(response!.ToBytes()).ReadUInt());

        var requestNotify = Assert.Single(
            target.Sent,
            packet => packet.Type == PacketType.NotifyRequestFriendList
        );
        var requestReader = new PacketReader(requestNotify.Payload);
        Assert.Equal(1u, requestReader.ReadUInt());
        Assert.Equal("character-1", requestReader.ReadString());

        var answerHandler = new AreaRequestFriendListAnswerHandler(friends, state);
        var answerWriter = new PacketWriter();
        answerWriter.Write(0u);
        await answerHandler.HandleAsync(answerWriter.ToBytes(), target, ct);

        await using (var verify = new MainContext(options))
        {
            var friendship = await verify.Friendships.SingleAsync(ct);
            Assert.Equal(1, friendship.CharacterIdLow);
            Assert.Equal(2, friendship.CharacterIdHigh);
        }
        Assert.Contains(
            requester.Sent,
            packet =>
                packet.Type == PacketType.NotifyAddFriendListResult
                && new PacketReader(packet.Payload).ReadUInt() == 0
        );

        requester.Sent.Clear();
        var listHandler = new AreaFriendGetListDataHandler(friends, state);
        await listHandler.HandleAsync(ReadOnlyMemory<byte>.Empty, requester, ct);
        var list = Assert.Single(
            requester.Sent,
            packet => packet.Type == PacketType.FriendGetListDataResponse
        );
        var listReader = new PacketReader(list.Payload);
        Assert.Equal(0u, listReader.ReadUInt());
        Assert.Equal(1u, listReader.ReadUInt());
        Assert.Equal(2u, listReader.ReadUInt());
        Assert.Equal("character-2", listReader.ReadString());
        Assert.Equal(1u, listReader.ReadUInt());
        Assert.Equal(1, listReader.ReadByte());
        Assert.Equal(0u, listReader.ReadUInt());
    }
}
