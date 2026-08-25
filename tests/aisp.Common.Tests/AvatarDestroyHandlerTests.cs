using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.Handlers.Msg;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AvatarDestroyHandlerTests
{
    [Fact]
    public async Task Destroy_RemovesRestrictedCircleDataAndCharacter()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var user = CreateUser(1, 101, "owner", "Owner");
            var otherUser = CreateUser(2, 202, "other", "Other");
            user.Characters.Single()
                .Rooms.Add(
                    new Room
                    {
                        Id = 1001,
                        Name = "Owner Room",
                        IsDefault = true,
                    }
                );

            await using (var db = new MainContext(options))
            {
                db.Users.AddRange(user, otherUser);
                db.Circles.AddRange(
                    new Circle
                    {
                        Id = 10,
                        Name = "Owned Circle",
                        LeaderCharacterId = 101,
                        Members =
                        [
                            new CircleMember { CharacterId = 101 },
                            new CircleMember { CharacterId = 202 },
                        ],
                    },
                    new Circle
                    {
                        Id = 20,
                        Name = "Other Circle",
                        LeaderCharacterId = 202,
                        Members = [new CircleMember { CharacterId = 202 }],
                        JoinRequests =
                        [
                            new CircleJoinRequest
                            {
                                RequesterCharacterId = 202,
                                TargetCharacterId = 101,
                            },
                        ],
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                // Reproduce the legacy Character.CircleId back-reference used by existing data.
                user.Characters.Single().CircleId = 10;
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.Single(),
                CharacterId = 101,
            };
            var handler = new AvatarDestroyHandler(
                new MainContext(options),
                NullLogger<AvatarDestroyHandler>.Instance
            );

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            await using var verifyDb = new MainContext(options);
            Assert.False(
                await verifyDb.Characters.AnyAsync(
                    character => character.Id == 101,
                    TestContext.Current.CancellationToken
                )
            );
            Assert.False(
                await verifyDb.Circles.AnyAsync(
                    circle => circle.Id == 10,
                    TestContext.Current.CancellationToken
                )
            );
            Assert.True(
                await verifyDb.Circles.AnyAsync(
                    circle => circle.Id == 20,
                    TestContext.Current.CancellationToken
                )
            );
            Assert.Empty(
                await verifyDb.CircleJoinRequests.ToListAsync(TestContext.Current.CancellationToken)
            );
            Assert.Empty(user.Characters);
            Assert.Null(session.Character);
            Assert.Equal(0u, session.CharacterId);
            Assert.Equal(PacketType.AvatarDestroyResponse, Assert.Single(session.Sent).Type);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static User CreateUser(int userId, int characterId, string username, string name)
    {
        var user = new User { Id = userId, Username = username };
        user.SetPassword("pw");
        user.Characters.Add(
            new Character
            {
                Id = characterId,
                UserId = userId,
                Name = name,
                Birthdate = new DateTime(2000, 1, 1),
            }
        );
        return user;
    }
}
