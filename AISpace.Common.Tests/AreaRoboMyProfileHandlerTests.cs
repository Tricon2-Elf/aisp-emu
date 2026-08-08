using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Tests;

public sealed class AreaRoboMyProfileHandlerTests
{
    [Fact]
    public async Task EditAndGetOwnedProfile_PersistTextAndKeepServerOwnedMetadata()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 7, TestContext.Current.CancellationToken);
            await SeedRoboAsync(options, 7, 2, 44);

            var createdAt = DateTime.UtcNow.AddDays(-10.5);
            var previousUpdatedAt = DateTime.UtcNow.AddDays(-1);
            await using (var seedDb = new MainContext(options))
            {
                var robo = await seedDb.Robos.SingleAsync(
                    x => x.CharacterId == 7 && x.RoboId == 2,
                    TestContext.Current.CancellationToken
                );
                robo.CreatedAt = createdAt;
                robo.UpdatedAt = previousUpdatedAt;
                robo.ProfileUnknownDword04 = 0x11223344;
                robo.ProfileUnknownDword08 = 0x55667788;
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var editedProfile = new ProfileData(
                "Robots",
                "Tea",
                "Maps",
                "Building things",
                "Hot and strong",
                "Finding shortcuts",
                "A persistent Robo profile"
            );
            var echoedMetadata = new AvatarProfileMetadata(9999, 0xAAAAAAAA, 0xBBBBBBBB);
            var editSession = new CapturingPlayerSession { CharacterId = 7 };
            await using (var editDb = new MainContext(options))
            {
                var handler = new AreaEditRoboMyProfileHandler(editDb);
                Assert.IsAssignableFrom<IRequiresAuthenticatedSession>(handler);

                await handler.HandleAsync(
                    BuildEditPayload(2, editedProfile, 123, echoedMetadata),
                    editSession,
                    TestContext.Current.CancellationToken
                );
            }

            var editResponse = Assert.Single(editSession.Sent);
            Assert.Equal(PacketType.EditRoboMyProfileResponse, editResponse.Type);
            var editReader = new PacketReader(editResponse.Payload);
            Assert.Equal(0u, editReader.ReadUInt());

            await using (var inspectionDb = new MainContext(options))
            {
                var stored = await inspectionDb
                    .Robos.AsNoTracking()
                    .SingleAsync(
                        x => x.CharacterId == 7 && x.RoboId == 2,
                        TestContext.Current.CancellationToken
                    );
                Assert.Equal(editedProfile.Like1, stored.Like1);
                Assert.Equal(editedProfile.Like2, stored.Like2);
                Assert.Equal(editedProfile.Like3, stored.Like3);
                Assert.Equal(editedProfile.LikeDesc1, stored.LikeDesc1);
                Assert.Equal(editedProfile.LikeDesc2, stored.LikeDesc2);
                Assert.Equal(editedProfile.LikeDesc3, stored.LikeDesc3);
                Assert.Equal(editedProfile.AvatarDesc, stored.ProfileDescription);
                Assert.Equal(0x11223344u, stored.ProfileUnknownDword04);
                Assert.Equal(0x55667788u, stored.ProfileUnknownDword08);
                Assert.Equal(44u, stored.JobId);
                Assert.True(stored.UpdatedAt > previousUpdatedAt);
            }

            var getSession = new CapturingPlayerSession { CharacterId = 7 };
            await using (var getDb = new MainContext(options))
            {
                var handler = new AreaGetMyRoboMyProfileDataHandler(getDb);
                Assert.IsAssignableFrom<IRequiresAuthenticatedSession>(handler);

                await handler.HandleAsync(
                    BuildGetPayload(2),
                    getSession,
                    TestContext.Current.CancellationToken
                );
            }

            var getResponse = Assert.Single(getSession.Sent);
            Assert.Equal(PacketType.GetMyRoboMyProfileDataResponse, getResponse.Type);
            Assert.Equal(sizeof(uint) + AvatarProfile.WireSize, getResponse.Payload.Length);
            var (result, profile, metadata) = ReadGetResponse(getResponse.Payload);
            Assert.Equal(0u, result);
            Assert.Equal(editedProfile, profile);
            Assert.Equal(10u, metadata.PlayDurationDays);
            Assert.Equal(0x11223344u, metadata.UnknownDword04);
            Assert.Equal(0x55667788u, metadata.UnknownDword08);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetAndEditUnownedProfile_ReturnFailureWithoutLeakingOrMutating()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);
            await SeedRoboAsync(options, 2, 1, 44);

            await using (var seedDb = new MainContext(options))
            {
                var robo = await seedDb.Robos.SingleAsync(
                    x => x.CharacterId == 2 && x.RoboId == 1,
                    TestContext.Current.CancellationToken
                );
                robo.Like1 = "Secret like";
                robo.ProfileDescription = "Secret profile";
                robo.ProfileUnknownDword04 = 123;
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var getSession = new CapturingPlayerSession { CharacterId = 1 };
            await using (var getDb = new MainContext(options))
            {
                await new AreaGetMyRoboMyProfileDataHandler(getDb).HandleAsync(
                    BuildGetPayload(1),
                    getSession,
                    TestContext.Current.CancellationToken
                );
            }

            var getResponse = Assert.Single(getSession.Sent);
            Assert.Equal(sizeof(uint) + AvatarProfile.WireSize, getResponse.Payload.Length);
            var (result, profile, metadata) = ReadGetResponse(getResponse.Payload);
            Assert.Equal(1u, result);
            Assert.Equal(string.Empty, profile.Like1);
            Assert.Equal(string.Empty, profile.AvatarDesc);
            Assert.Equal(default, metadata);

            var editSession = new CapturingPlayerSession { CharacterId = 1 };
            var attemptedProfile = new ProfileData(
                "Changed",
                "Changed",
                "Changed",
                "Changed",
                "Changed",
                "Changed",
                "Changed"
            );
            await using (var editDb = new MainContext(options))
            {
                await new AreaEditRoboMyProfileHandler(editDb).HandleAsync(
                    BuildEditPayload(1, attemptedProfile, 999, new AvatarProfileMetadata(1, 2, 3)),
                    editSession,
                    TestContext.Current.CancellationToken
                );
            }

            var editResponse = Assert.Single(editSession.Sent);
            Assert.Equal(PacketType.EditRoboMyProfileResponse, editResponse.Type);
            var editReader = new PacketReader(editResponse.Payload);
            Assert.Equal(1u, editReader.ReadUInt());

            await using var inspectionDb = new MainContext(options);
            var stored = await inspectionDb
                .Robos.AsNoTracking()
                .SingleAsync(
                    x => x.CharacterId == 2 && x.RoboId == 1,
                    TestContext.Current.CancellationToken
                );
            Assert.Equal("Secret like", stored.Like1);
            Assert.Equal("Secret profile", stored.ProfileDescription);
            Assert.Equal(123u, stored.ProfileUnknownDword04);
            Assert.Equal(44u, stored.JobId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task NormalRoboUpsert_PreservesExistingProfile()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 3, TestContext.Current.CancellationToken);
            await SeedRoboAsync(options, 3, 1, 44);

            await using (var profileDb = new MainContext(options))
            {
                var robo = await profileDb.Robos.SingleAsync(
                    x => x.CharacterId == 3 && x.RoboId == 1,
                    TestContext.Current.CancellationToken
                );
                robo.Like1 = "Preserved";
                robo.ProfileDescription = "Keep me";
                robo.ProfileUnknownDword08 = 77;
                await profileDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using (var updateDb = new MainContext(options))
            {
                var repository = new RoboRepository(updateDb);
                var robo = Assert.IsType<RoboData>(
                    await repository.GetAsync(3, 1, TestContext.Current.CancellationToken)
                );
                robo.Character.Name = "Updated Robo";
                await repository.UpsertAsync(3, robo, TestContext.Current.CancellationToken);
            }

            await using var inspectionDb = new MainContext(options);
            var stored = await inspectionDb
                .Robos.AsNoTracking()
                .SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal("Updated Robo", stored.Name);
            Assert.Equal("Preserved", stored.Like1);
            Assert.Equal("Keep me", stored.ProfileDescription);
            Assert.Equal(77u, stored.ProfileUnknownDword08);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static async Task SeedRoboAsync(
        DbContextOptions<MainContext> options,
        int characterId,
        uint roboId,
        uint jobId
    )
    {
        var objectId = RoboRepository.GetObjectId(checked((uint)characterId), roboId);
        var character = new CharaData(objectId, 1002011, $"Robo {roboId}")
        {
            Visual = new CharaVisual(BloodType.A, 1, 1, 0, objectId, 0, 10930010),
            JobId = jobId,
        };
        await using var db = new MainContext(options);
        await new RoboRepository(db).UpsertAsync(
            characterId,
            new RoboData(roboId, character, state: 1)
            {
                OwnerAvatarId = checked((uint)characterId),
            },
            TestContext.Current.CancellationToken
        );
    }

    private static byte[] BuildGetPayload(uint roboId)
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        return writer.ToBytes();
    }

    private static byte[] BuildEditPayload(
        uint roboId,
        ProfileData profile,
        uint jobId,
        AvatarProfileMetadata metadata
    )
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        AvatarProfile.Write(writer, profile, metadata);
        writer.Write(jobId);
        return writer.ToBytes();
    }

    private static (
        uint Result,
        ProfileData Profile,
        AvatarProfileMetadata Metadata
    ) ReadGetResponse(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var result = reader.ReadUInt();
        var profile = AvatarProfile.Read(ref reader, out var metadata);
        return (result, profile, metadata);
    }
}
