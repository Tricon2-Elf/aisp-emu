using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
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
    public async Task ArbitraryTag_IsReturnedAfterSavingAndReconnecting()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 2, ct);

        await using (var saveDb = new MainContext(options))
        {
            var saveHandler = new AreaFriendLinkTagChangeHandler(
                new FriendRepository(saveDb),
                WordFilter.FromTerms([])
            );
            var session = new CapturingPlayerSession
            {
                CharacterId = 2,
                User = await saveDb.Users.SingleAsync(ct),
            };

            var result = await saveHandler.HandleAsync(
                new FriendLinkTagChangeRequest(3, "Anime"),
                session,
                ct
            );

            Assert.NotNull(result);
            Assert.Equal(4u, new PacketReader(result!.ToBytes()).ReadUInt());
        }

        await using var reconnectDb = new MainContext(options);
        var reconnectSession = new CapturingPlayerSession
        {
            CharacterId = 2,
            User = await reconnectDb.Users.SingleAsync(ct),
        };
        var getHandler = new AreaFriendLinkTagGetHandler(new FriendRepository(reconnectDb));
        var request = new PacketWriter();
        request.Write(4u);

        await getHandler.HandleAsync(request.ToBytes(), reconnectSession, ct);

        var response = Assert.Single(
            reconnectSession.Sent,
            packet => packet.Type == PacketType.FriendLinkTagGetResponse
        );
        var reader = new PacketReader(response.Payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal("Anime", reader.ReadFixedString(61));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
    }

    [Fact]
    public async Task ArbitraryTag_BlockedWordIsRejectedAndNotSaved()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 2, ct);

        await using var db = new MainContext(options);
        var handler = new AreaFriendLinkTagChangeHandler(
            new FriendRepository(db),
            WordFilter.FromTerms(["blockedword"])
        );
        var session = new CapturingPlayerSession
        {
            CharacterId = 2,
            User = await db.Users.SingleAsync(ct),
        };

        var result = await handler.HandleAsync(
            new FriendLinkTagChangeRequest(0, "BLOCKED-WORD"),
            session,
            ct
        );

        Assert.NotNull(result);
        Assert.Equal(0u, new PacketReader(result!.ToBytes()).ReadUInt());
        Assert.Empty(await db.FriendLinkTags.ToListAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Accept_RejectsWhenEitherParticipantReachedFriendLimit(bool targetAtLimit)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        var ct = TestContext.Current.CancellationToken;

        await using (var seed = new MainContext(options))
        {
            var user = new User { Id = 1, Username = "friend-limit" };
            user.SetPassword("pw");
            for (var id = 1; id <= FriendRepository.MaxFriends + 2; id++)
            {
                user.Characters.Add(
                    new Character
                    {
                        Id = id,
                        Name = $"limit-character-{id}",
                        Birthdate = new DateTime(2000, 1, 1),
                    }
                );
            }
            seed.Users.Add(user);

            var fullCharacterId = targetAtLimit ? 2 : 1;
            for (var id = 3; id <= FriendRepository.MaxFriends + 2; id++)
            {
                seed.Friendships.Add(
                    new Friendship
                    {
                        CharacterIdLow = Math.Min(fullCharacterId, id),
                        CharacterIdHigh = Math.Max(fullCharacterId, id),
                    }
                );
            }
            seed.FriendRequests.Add(
                new FriendRequest
                {
                    RequesterCharacterId = 1,
                    TargetCharacterId = 2,
                    Status = FriendRequestStatus.Pending,
                }
            );
            await seed.SaveChangesAsync(ct);
        }

        await using var db = new MainContext(options);
        var result = await new FriendRepository(db).AnswerAsync(2, accept: true, ct);

        Assert.Equal(FriendResult.LimitReached, result.Result);
        Assert.False(
            await db.Friendships.AnyAsync(
                friendship => friendship.CharacterIdLow == 1 && friendship.CharacterIdHigh == 2,
                ct
            )
        );
        var request = await db.FriendRequests.SingleAsync(ct);
        Assert.Equal(FriendRequestStatus.Rejected, request.Status);
        Assert.NotNull(request.ResolvedAt);
    }

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
        await db
            .Rooms.Where(room => room.OwnerCharacterId == 2)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(room => room.Security, MyRoomSecurity.FriendsOnly),
                ct
            );
        Assert.DoesNotContain(
            await new MyRoomRepository(db).GetCandidateVisitRoomsAsync(1, 100, ct),
            room => room.OwnerCharacterId == 2
        );

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
        Assert.Equal(16, requestNotify.Payload.Length);

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
        var rooms = await new MyRoomRepository(db).GetCandidateVisitRoomsAsync(1, 100, ct);
        Assert.Contains(rooms, room => room.OwnerCharacterId == 2);
        Assert.True(await new MyRoomRepository(db).AreFriendsAsync(1, 2, ct));

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
        Assert.Equal(58, list.Payload.Length);
        var listReader = new PacketReader(list.Payload);
        Assert.Equal(0u, listReader.ReadUInt());
        Assert.Equal(1u, listReader.ReadUInt());
        Assert.Equal(2u, listReader.ReadUInt());
        Assert.Equal("character-2", listReader.ReadFixedString(37));
        Assert.Equal(1u, listReader.ReadUInt());
        Assert.Equal(1, listReader.ReadByte());
        Assert.Equal(0u, listReader.ReadUInt());

        requester.Sent.Clear();
        await FriendNotifyHelper.NotifyLogoutAsync(friends, state, 2, ct);
        var logout = Assert.Single(
            requester.Sent,
            packet => packet.Type == PacketType.NotifyFriendListAvatarLogout
        );
        Assert.Equal(2u, new PacketReader(logout.Payload).ReadUInt());

        requester.Sent.Clear();
        await FriendNotifyHelper.NotifyLoginAsync(friends, state, 2, ct);
        var login = Assert.Single(
            requester.Sent,
            packet => packet.Type == PacketType.NotifyFriendListAvatarLogin
        );
        Assert.Equal(2u, new PacketReader(login.Payload).ReadUInt());
    }
}
