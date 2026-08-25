using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Msg;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

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

        Assert.True(session.Sent.Count >= 2);
        Assert.Equal(PacketType.CircleGetDataResponse, session.Sent[0].Type);
        Assert.Equal(PacketType.CircleNotifyMember, session.Sent[1].Type);

        var response = session.Sent[0];
        var reader = new PacketReader(response.Payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        var circle = CircleData.Read(ref reader);
        Assert.Equal((ulong)created.Circle.Id, circle.Id);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(CircleMemberData.RoleMember, reader.ReadUInt());

        var roster = new PacketReader(session.Sent[1].Payload);
        Assert.Equal((ulong)created.Circle.Id, roster.ReadULong());
        Assert.Equal(2u, roster.ReadUInt());
        var leader = CircleMemberData.Read(ref roster);
        var member = CircleMemberData.Read(ref roster);
        Assert.Equal(1u, leader.AvatarId);
        Assert.Equal(2u, member.AvatarId);
        Assert.Equal("character-1", leader.Name);
        Assert.Equal("character-2", member.Name);
    }

    [Fact]
    public async Task ChatPost_ForwardsToAllOnlineCircleMembers()
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
            Character = db.Characters.First(c => c.Id == 1),
            User = db.Users.First(u => u.Id == 1),
        };
        var memberInChat = new CapturingPlayerSession
        {
            CharacterId = 2,
            Character = db.Characters.First(c => c.Id == 2),
            User = db.Users.First(u => u.Id == 2),
        };
        var memberNotInChat = new CapturingPlayerSession
        {
            CharacterId = 3,
            Character = db.Characters.First(c => c.Id == 3),
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
        Assert.Contains(memberNotInChat.Sent, p => p.Type == PacketType.CircleChatForwardNotify);
        Assert.Contains(memberNotInChat.Sent, p => p.Type == PacketType.CircleNotifyMember);
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
    public async Task AnswerInvite_RejectsWhenCircleIsFull()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
        await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        var created = await circles.CreateAsync(
            1,
            "Full",
            0,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(CircleResult.Ok, created.Result);

        var now = DateTime.UtcNow;
        for (var i = 0; i < CircleRepository.MaxMembersPerCircle - 1; i++)
        {
            var characterId = 1000 + i;
            await TestDb.SeedCharacterAsync(
                options,
                characterId,
                TestContext.Current.CancellationToken
            );
            db.CircleMembers.Add(
                new CircleMember
                {
                    CircleId = created.Circle!.Id,
                    CharacterId = characterId,
                    AuthLevel = CircleMemberData.RoleMember,
                    JoinedAt = now,
                }
            );
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            CircleRepository.MaxMembersPerCircle,
            await db.CircleMembers.CountAsync(
                x => x.CircleId == created.Circle!.Id,
                TestContext.Current.CancellationToken
            )
        );

        // Pending invite created while full must still fail at accept time.
        db.CircleJoinRequests.Add(
            new CircleJoinRequest
            {
                CircleId = created.Circle!.Id,
                RequesterCharacterId = 1,
                TargetCharacterId = 2,
                Status = CircleJoinRequestStatus.Pending,
                CreatedAt = now,
            }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var answer = await circles.AnswerInviteAsync(
            2,
            true,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(CircleResult.LimitReached, answer.Result);
        Assert.Equal(
            CircleRepository.MaxMembersPerCircle,
            await db.CircleMembers.CountAsync(
                x => x.CircleId == created.Circle!.Id,
                TestContext.Current.CancellationToken
            )
        );
        Assert.False(
            await db.CircleMembers.AnyAsync(
                x => x.CircleId == created.Circle.Id && x.CharacterId == 2,
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task MembershipLimit_IsFifteen()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
        for (var i = 0; i < 15; i++)
            await TestDb.SeedCharacterAsync(
                options,
                100 + i,
                TestContext.Current.CancellationToken
            );

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

    [Fact]
    public async Task Kick_RemovesKickedClientFromCircleChatSessions()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
        await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        var created = await circles.CreateAsync(
            1,
            "KickChat",
            0,
            TestContext.Current.CancellationToken
        );
        await circles.InviteAsync(1, 2, created.Circle!.Id, TestContext.Current.CancellationToken);
        await circles.AnswerInviteAsync(2, true, TestContext.Current.CancellationToken);

        var state = new SharedState();
        var leader = new CapturingPlayerSession
        {
            CharacterId = 1,
            User = db.Users.First(u => u.Id == 1),
        };
        var member = new CapturingPlayerSession
        {
            CharacterId = 2,
            User = db.Users.First(u => u.Id == 2),
        };
        state.RegisterClient(ServerType.Msg, leader);
        state.RegisterClient(ServerType.Msg, member);
        state.EnterCircleChat(leader.ConnectionId, created.Circle.Id);
        state.EnterCircleChat(member.ConnectionId, created.Circle.Id);

        var handler = new CircleMemberKickHandler(circles, state);
        var response = await handler.HandleAsync(
            new CircleMemberKickRequest { CircleId = (ulong)created.Circle.Id, AvatarId = 2 },
            leader,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(response);
        Assert.Equal(0u, new PacketReader(response!.ToBytes()).ReadUInt());
        Assert.False(state.TryGetCircleChat(member.ConnectionId, out var memberChat));
        Assert.True(state.TryGetCircleChat(leader.ConnectionId, out var leaderChat));
        Assert.Equal(created.Circle.Id, leaderChat);
        Assert.DoesNotContain(
            state.GetCircleChatClients(created.Circle.Id),
            c => c.ConnectionId == member.ConnectionId
        );
    }

    [Fact]
    public async Task JoinAnswer_Accept_NotifiesAddMemberWithCharacterName()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
        await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        var created = await circles.CreateAsync(
            1,
            "Named",
            0,
            TestContext.Current.CancellationToken
        );
        await circles.InviteAsync(1, 2, created.Circle!.Id, TestContext.Current.CancellationToken);

        var state = new SharedState();
        var leader = new CapturingPlayerSession
        {
            CharacterId = 1,
            Character = db.Characters.First(c => c.Id == 1),
            User = db.Users.First(u => u.Id == 1),
        };
        var invitee = new CapturingPlayerSession
        {
            CharacterId = 2,
            Character = db.Characters.First(c => c.Id == 2),
            User = db.Users.First(u => u.Id == 2),
        };
        state.RegisterClient(ServerType.Msg, leader);
        state.RegisterClient(ServerType.Msg, invitee);

        var handler = new CircleMemberJoinAnswerHandler(circles, state);
        var writer = new PacketWriter();
        writer.Write(1u); // accept
        await handler.HandleAsync(writer.ToBytes(), invitee, TestContext.Current.CancellationToken);

        var add = leader.Sent.Single(p => p.Type == PacketType.CircleNotifyAddMember);
        var reader = new PacketReader(add.Payload);
        Assert.Equal((ulong)created.Circle!.Id, reader.ReadULong());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal("character-2", reader.ReadString());
    }

    [Fact]
    public async Task MemberLogin_SendsRosterWithAlreadyOnlinePeers()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
        await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        var created = await circles.CreateAsync(
            1,
            "Online",
            0,
            TestContext.Current.CancellationToken
        );
        await circles.InviteAsync(1, 2, created.Circle!.Id, TestContext.Current.CancellationToken);
        await circles.AnswerInviteAsync(2, true, TestContext.Current.CancellationToken);

        var state = new SharedState();
        var alreadyOnline = new CapturingPlayerSession
        {
            CharacterId = 1,
            Character = db.Characters.First(c => c.Id == 1),
            User = db.Users.First(u => u.Id == 1),
        };
        var loggingIn = new CapturingPlayerSession
        {
            CharacterId = 2,
            Character = db.Characters.First(c => c.Id == 2),
            User = db.Users.First(u => u.Id == 2),
        };
        state.RegisterClient(ServerType.Msg, alreadyOnline);
        state.RegisterClient(ServerType.Msg, loggingIn);

        await CircleNotifyHelper.NotifyMemberLoginAsync(
            circles,
            state,
            2,
            TestContext.Current.CancellationToken
        );

        Assert.Contains(alreadyOnline.Sent, p => p.Type == PacketType.CircleNotifyMemberLogin);
        var roster = loggingIn.Sent.Single(p => p.Type == PacketType.CircleNotifyMember);
        var reader = new PacketReader(roster.Payload);
        Assert.Equal((ulong)created.Circle!.Id, reader.ReadULong());
        Assert.Equal(2u, reader.ReadUInt()); // member count
        CircleMemberData.Read(ref reader);
        CircleMemberData.Read(ref reader);
        Assert.Equal(2u, reader.ReadUInt()); // login flag count
        Assert.Equal(1, reader.ReadByte()); // member 1 online
        Assert.Equal(1, reader.ReadByte()); // member 2 online
    }
}
