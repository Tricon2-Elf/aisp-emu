using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Msg;
using aisp.Common.Services;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AvatarCreationFlowTests
{
    [Fact]
    public async Task AvatarCreate_HydratesMsgSession_ForImmediateAvatarGetData()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = new User { Username = "new-character-user" };
            user.SetPassword("pw");

            await using (var seedDb = new MainContext(options))
            {
                seedDb.Users.Add(user);
                foreach (var itemId in DefaultClothingItems.Female)
                {
                    seedDb.Items.Add(new Item { Id = itemId, Name = $"item-{itemId}" });
                }

                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = new CapturingPlayerSession
            {
                UserId = user.Id,
                User = new User { Id = user.Id, Username = user.Username },
            };

            await using var runDb = new MainContext(options);
            var characterRepository = new CharacterRepository(
                runDb,
                NullLogger<CharacterRepository>.Instance
            );
            var createHandler = CreateAvatarCreateHandler(options, characterRepository);
            var createResponse = await createHandler.HandleAsync(
                new AvatarCreateRequest
                {
                    AvatarName = "Fresh Avatar",
                    modelId = 1_002_011,
                    visual = new CharaVisual(BloodType.AB, 7, 26, 2, 0, 4, 2_002_031),
                    slotId = 0,
                },
                session,
                TestContext.Current.CancellationToken
            );

            Assert.NotNull(createResponse);
            Assert.Equal(0u, new PacketReader(createResponse.ToBytes()).ReadUInt());

            var sessionCharacter = Assert.Single(session.User.Characters);
            Assert.Equal("Fresh Avatar", sessionCharacter.Name);
            Assert.Equal(1_002_011u, sessionCharacter.ModelId);
            Assert.Equal(4, sessionCharacter.Equipment.Count);

            Assert.Collection(
                session.Sent,
                avatarPacket =>
                {
                    Assert.Equal(PacketType.AvatarDataResponse, avatarPacket.Type);
                    AssertAvatarData(avatarPacket.Payload, sessionCharacter);
                }
            );

            session.Sent.Clear();
            var getDataHandler = new AvatarGetDataHandler(
                NullLogger<AvatarGetDataHandler>.Instance,
                characterRepository
            );
            await getDataHandler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                avatarPacket =>
                {
                    Assert.Equal(PacketType.AvatarDataResponse, avatarPacket.Type);
                    AssertAvatarData(avatarPacket.Payload, sessionCharacter);
                },
                completionPacket =>
                {
                    Assert.Equal(PacketType.AvatarGetDataResponse, completionPacket.Type);
                    Assert.Equal(0u, new PacketReader(completionPacket.Payload).ReadUInt());
                }
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static void AssertAvatarData(byte[] payload, Character character)
    {
        var reader = new PacketReader(payload);
        Assert.Equal((uint)character.Id, reader.ReadUInt());
        Assert.Equal("Fresh Avatar", reader.ReadString());
        Assert.Equal(1_002_011u, reader.ReadUInt());
        var visual = CharaVisual.FromBytes(reader.ReadBytes(19));
        Assert.Equal(BloodType.AB, visual.BloodType);
        Assert.Equal(2u, visual.Gender);
        Assert.Equal(1u, visual.VisualId);
        Assert.Equal(4, visual.Face);
        Assert.Equal(2_002_031u, visual.Hairstyle);
    }

    [Fact]
    public async Task AvatarCreate_RejectsBlockedName_WithoutPersisting()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = new User { Username = "blocked-name-user" };
            user.SetPassword("pw");

            await using (var seedDb = new MainContext(options))
            {
                seedDb.Users.Add(user);
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = new CapturingPlayerSession
            {
                UserId = user.Id,
                User = new User { Id = user.Id, Username = user.Username },
            };

            await using var runDb = new MainContext(options);
            var characterRepository = new CharacterRepository(
                runDb,
                NullLogger<CharacterRepository>.Instance
            );
            var createHandler = CreateAvatarCreateHandler(
                options,
                characterRepository,
                WordFilter.FromTerms(["faggot"])
            );
            var createResponse = await createHandler.HandleAsync(
                new AvatarCreateRequest
                {
                    AvatarName = "Faggot",
                    modelId = 1_002_011,
                    visual = new CharaVisual(BloodType.A, 1, 1, 1, 0, 1, 1),
                    slotId = 0,
                },
                session,
                TestContext.Current.CancellationToken
            );

            Assert.NotNull(createResponse);
            Assert.Equal(1u, new PacketReader(createResponse.ToBytes()).ReadUInt());
            Assert.Empty(session.User.Characters);
            Assert.Empty(session.Sent);

            await using var verifyDb = new MainContext(options);
            Assert.False(
                await verifyDb.Characters.AnyAsync(
                    c => c.UserId == user.Id,
                    TestContext.Current.CancellationToken
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarCreate_RejectsDuplicateName_WithoutPersisting()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var owner = new User { Username = "name-owner" };
            owner.SetPassword("pw");
            var creator = new User { Username = "name-creator" };
            creator.SetPassword("pw");

            await using (var seedDb = new MainContext(options))
            {
                seedDb.Users.Add(owner);
                seedDb.Users.Add(creator);
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
                seedDb.Characters.Add(new Character { UserId = owner.Id, Name = "Taken Name" });
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = new CapturingPlayerSession
            {
                UserId = creator.Id,
                User = new User { Id = creator.Id, Username = creator.Username },
            };

            await using var runDb = new MainContext(options);
            var characterRepository = new CharacterRepository(
                runDb,
                NullLogger<CharacterRepository>.Instance
            );
            var createHandler = CreateAvatarCreateHandler(options, characterRepository);
            var createResponse = await createHandler.HandleAsync(
                new AvatarCreateRequest
                {
                    AvatarName = "Taken Name",
                    modelId = 1_002_011,
                    visual = new CharaVisual(BloodType.A, 1, 1, 1, 0, 1, 1),
                    slotId = 0,
                },
                session,
                TestContext.Current.CancellationToken
            );

            Assert.NotNull(createResponse);
            Assert.Equal(1u, new PacketReader(createResponse.ToBytes()).ReadUInt());
            Assert.Empty(session.User.Characters);
            Assert.Empty(session.Sent);

            await using var verifyDb = new MainContext(options);
            Assert.Equal(
                1,
                await verifyDb.Characters.CountAsync(
                    c => c.Name == "Taken Name",
                    TestContext.Current.CancellationToken
                )
            );
            Assert.False(
                await verifyDb.Characters.AnyAsync(
                    c => c.UserId == creator.Id,
                    TestContext.Current.CancellationToken
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarCreate_StaffUser_AddsNewCharacterToModeratorsCircle()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var admin = new User { Username = "bootstrap-admin", Role = UserRole.ServerAdmin };
            admin.SetPassword("pw");

            await using (var seedDb = new MainContext(options))
            {
                seedDb.Users.Add(admin);
                foreach (var itemId in DefaultClothingItems.Female)
                    seedDb.Items.Add(new Item { Id = itemId, Name = $"item-{itemId}" });
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = new CapturingPlayerSession
            {
                UserId = admin.Id,
                User = new User
                {
                    Id = admin.Id,
                    Username = admin.Username,
                    Role = UserRole.ServerAdmin,
                },
            };

            await using var runDb = new MainContext(options);
            var characterRepository = new CharacterRepository(
                runDb,
                NullLogger<CharacterRepository>.Instance
            );
            var createHandler = CreateAvatarCreateHandler(options, characterRepository);
            var createResponse = await createHandler.HandleAsync(
                new AvatarCreateRequest
                {
                    AvatarName = "Admin Avatar",
                    modelId = 1_002_011,
                    visual = new CharaVisual(BloodType.AB, 7, 26, 2, 0, 4, 2_002_031),
                    slotId = 0,
                },
                session,
                TestContext.Current.CancellationToken
            );

            Assert.NotNull(createResponse);
            Assert.Equal(0u, new PacketReader(createResponse.ToBytes()).ReadUInt());

            var created = Assert.Single(session.User.Characters);
            await using var verifyDb = new MainContext(options);
            var circle = Assert.Single(
                verifyDb.Circles.Where(c => c.Name == ModerationService.ModeratorsCircleName)
            );
            Assert.Equal(created.Id, circle.LeaderCharacterId);
            Assert.True(
                verifyDb.CircleMembers.Any(member =>
                    member.CircleId == circle.Id && member.CharacterId == created.Id
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static AvatarCreateHandler CreateAvatarCreateHandler(
        DbContextOptions<MainContext> options,
        ICharacterRepository characterRepository,
        IWordFilter? wordFilter = null
    ) =>
        new(
            NullLogger<AvatarCreateHandler>.Instance,
            characterRepository,
            wordFilter ?? WordFilter.FromTerms(Array.Empty<string>()),
            new ModerationService(
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new CircleRepository(new MainContext(options)),
                new MainContext(options),
                new SharedState(),
                NullLogger<ModerationService>.Instance
            )
        );
}
