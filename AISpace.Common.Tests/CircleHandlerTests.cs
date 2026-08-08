using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Msg;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public sealed class CircleHandlerTests
{
    [Fact]
    public async Task Create_PersistsCircleAndMembership()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        var state = new SharedState();
        var session = new CapturingPlayerSession { CharacterId = 1, User = db.Users.First() };
        state.RegisterClient(ServerType.Msg, session);

        var handler = new CircleCreateHandler(circles, state);
        var response = await handler.HandleAsync(
            new CircleCreateRequest("Alpha", 3),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(response);
        Assert.Equal(0u, response!.Result);
        Assert.NotNull(response.Circle);
        Assert.True(response.Circle!.Id > 0);

        await using var verify = new MainContext(options);
        var circle = await verify.Circles.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Alpha", circle.Name);
        Assert.Equal(3u, circle.MarkId);
        Assert.Equal(1, circle.LeaderCharacterId);
        Assert.Equal(
            1,
            await verify.CircleMembers.CountAsync(TestContext.Current.CancellationToken)
        );
        Assert.Contains(session.Sent, p => p.Type == PacketType.CircleNotifyMember);
    }

    [Fact]
    public async Task GetData_ReturnsAllMembershipsWithAuthLevels()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
        await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        var created = await circles.CreateAsync(
            1,
            "Alpha",
            1,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(CircleResult.Ok, created.Result);
        await circles.InviteAsync(1, 2, created.Circle!.Id, TestContext.Current.CancellationToken);
        await circles.AnswerInviteAsync(2, true, TestContext.Current.CancellationToken);

        var state = new SharedState();
        var session = new CapturingPlayerSession
        {
            CharacterId = 2,
            User = db.Users.First(u => u.Id == 2),
        };
        state.RegisterClient(ServerType.Msg, session);
        var handler = new CircleGetDataHandler(circles, state);
        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        var response = session.Sent.Single(p => p.Type == PacketType.CircleGetDataResponse);
        var reader = new PacketReader(response.Payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        var circle = CircleData.Read(ref reader);
        Assert.Equal((uint)created.Circle.Id, circle.Id);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(CircleMemberData.RoleMember, reader.ReadUInt());
    }

    [Fact]
    public async Task ChatPost_OnlyForwardsToActiveChatMembers()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
        await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);
        await TestDb.SeedCharacterAsync(options, 3, TestContext.Current.CancellationToken);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        var created = await circles.CreateAsync(
            1,
            "Chat",
            0,
            TestContext.Current.CancellationToken
        );
        await circles.InviteAsync(1, 2, created.Circle!.Id, TestContext.Current.CancellationToken);
        await circles.AnswerInviteAsync(2, true, TestContext.Current.CancellationToken);
        await circles.InviteAsync(1, 3, created.Circle.Id, TestContext.Current.CancellationToken);
        await circles.AnswerInviteAsync(3, true, TestContext.Current.CancellationToken);

        var state = new SharedState();
        var leader = new CapturingPlayerSession
        {
            CharacterId = 1,
            User = db.Users.First(u => u.Id == 1),
        };
        var memberInChat = new CapturingPlayerSession
        {
            CharacterId = 2,
            User = db.Users.First(u => u.Id == 2),
        };
        var memberNotInChat = new CapturingPlayerSession
        {
            CharacterId = 3,
            User = db.Users.First(u => u.Id == 3),
        };
        state.RegisterClient(ServerType.Msg, leader);
        state.RegisterClient(ServerType.Msg, memberInChat);
        state.RegisterClient(ServerType.Msg, memberNotInChat);
        state.EnterCircleChat(leader.ConnectionId, created.Circle.Id);
        state.EnterCircleChat(memberInChat.ConnectionId, created.Circle.Id);

        var handler = new CircleChatPostHandler(
            NullLogger<CircleChatPostHandler>.Instance,
            circles,
            state
        );
        var writer = new PacketWriter();
        writer.Write(9u);
        writer.Write("hello", "utf-8");
        await handler.HandleAsync(writer.ToBytes(), leader, TestContext.Current.CancellationToken);

        Assert.Contains(leader.Sent, p => p.Type == PacketType.CircleChatPostResponse);
        Assert.Contains(memberInChat.Sent, p => p.Type == PacketType.CircleChatForwardNotify);
        Assert.DoesNotContain(
            memberNotInChat.Sent,
            p => p.Type == PacketType.CircleChatForwardNotify
        );
    }

    [Fact]
    public async Task ResignLeader_PromotesEarliestCoreThenMember()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
        await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);
        await TestDb.SeedCharacterAsync(options, 3, TestContext.Current.CancellationToken);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        var created = await circles.CreateAsync(
            1,
            "Lead",
            0,
            TestContext.Current.CancellationToken
        );
        await circles.InviteAsync(1, 2, created.Circle!.Id, TestContext.Current.CancellationToken);
        await circles.AnswerInviteAsync(2, true, TestContext.Current.CancellationToken);
        await circles.InviteAsync(1, 3, created.Circle.Id, TestContext.Current.CancellationToken);
        await circles.AnswerInviteAsync(3, true, TestContext.Current.CancellationToken);
        await circles.SetCoreAuthorityAsync(
            1,
            created.Circle.Id,
            3,
            1,
            TestContext.Current.CancellationToken
        );

        var result = await circles.ResignAsync(
            1,
            created.Circle.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(CircleResult.Ok, result.Result);
        Assert.Equal(3, result.NewLeaderCharacterId);

        var circle = await circles.GetByIdAsync(
            created.Circle.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(3, circle!.LeaderCharacterId);
    }

    [Fact]
    public async Task MembershipLimit_IsFifteen()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
        for (var i = 0; i < 15; i++)
            await TestDb.SeedCharacterAsync(options, 100 + i, TestContext.Current.CancellationToken);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        for (var i = 0; i < 15; i++)
        {
            var leaderId = 100 + i;
            var created = await circles.CreateAsync(
                leaderId,
                $"C{i}",
                0,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(CircleResult.Ok, created.Result);
            var invite = await circles.InviteAsync(
                leaderId,
                1,
                created.Circle!.Id,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(CircleResult.Ok, invite.Result);
            var answer = await circles.AnswerInviteAsync(
                1,
                true,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(CircleResult.Ok, answer.Result);
        }

        await TestDb.SeedCharacterAsync(options, 200, TestContext.Current.CancellationToken);
        var overflow = await circles.CreateAsync(
            200,
            "OverflowHost",
            0,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(CircleResult.Ok, overflow.Result);
        var blocked = await circles.InviteAsync(
            200,
            1,
            overflow.Circle!.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(CircleResult.LimitReached, blocked.Result);
    }

    [Fact]
    public async Task SharesAnyCircle_UsedByMyRoomAccess()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
        await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        var created = await circles.CreateAsync(
            1,
            "Room",
            0,
            TestContext.Current.CancellationToken
        );
        await circles.InviteAsync(1, 2, created.Circle!.Id, TestContext.Current.CancellationToken);
        await circles.AnswerInviteAsync(2, true, TestContext.Current.CancellationToken);

        Assert.True(
            await circles.SharesAnyCircleAsync(1, 2, TestContext.Current.CancellationToken)
        );
        var room = new Room { OwnerCharacterId = 1, Security = MyRoomSecurity.CircleMembersOnly };
        Assert.True(
            MyRoomAccess.CanEnter(
                room,
                2,
                await circles.SharesAnyCircleAsync(1, 2, TestContext.Current.CancellationToken)
            )
        );
    }
}
