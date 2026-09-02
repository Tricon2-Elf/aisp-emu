using aisp.Common.DAL;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public sealed class AreaMyProfileAvatarEditHandlerTests
{
    [Fact]
    public async Task Edit_PersistsCleanProfileText()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            var session = new CapturingPlayerSession { CharacterId = 42 };
            var profile = new ProfileData(
                "Tea",
                "Maps",
                "Robots",
                "Hot and strong",
                "Finding shortcuts",
                "Building things",
                "Hello there"
            );

            await using (var db = new MainContext(options))
            {
                var handler = new AreaMyProfileAvatarEditHandler(
                    db,
                    WordFilter.FromTerms(["faggot"])
                );
                await handler.HandleAsync(
                    BuildEditPayload(profile),
                    session,
                    TestContext.Current.CancellationToken
                );
            }

            var response = Assert.Single(session.Sent);
            Assert.Equal(PacketType.MyProfileAvatarEditResponse, response.Type);
            Assert.Equal(0u, new PacketReader(response.Payload).ReadUInt());

            await using var verify = new MainContext(options);
            var stored = await verify.Characters.SingleAsync(
                character => character.Id == 42,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(profile.Like1, stored.Like1);
            Assert.Equal(profile.LikeDesc1, stored.LikeDesc1);
            Assert.Equal(profile.AvatarDesc, stored.AvatarDesc);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Edit_RejectsBlockedProfileTextWithoutPersisting()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            var session = new CapturingPlayerSession { CharacterId = 42 };
            var profile = new ProfileData("", "", "", "", "", "", "I am a faggot");

            await using (var db = new MainContext(options))
            {
                var handler = new AreaMyProfileAvatarEditHandler(
                    db,
                    WordFilter.FromTerms(["faggot"])
                );
                await handler.HandleAsync(
                    BuildEditPayload(profile),
                    session,
                    TestContext.Current.CancellationToken
                );
            }

            var response = Assert.Single(session.Sent);
            Assert.Equal(PacketType.MyProfileAvatarEditResponse, response.Type);
            Assert.Equal(1u, new PacketReader(response.Payload).ReadUInt());

            await using var verify = new MainContext(options);
            var stored = await verify.Characters.SingleAsync(
                character => character.Id == 42,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(string.Empty, stored.AvatarDesc);
            Assert.Equal(string.Empty, stored.Like1);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static byte[] BuildEditPayload(ProfileData profile)
    {
        var writer = new PacketWriter();
        AvatarProfile.Write(writer, profile);
        return writer.ToBytes();
    }
}
