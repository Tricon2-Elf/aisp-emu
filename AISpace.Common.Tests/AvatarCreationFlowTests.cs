using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Msg;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

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
            var createHandler = new AvatarCreateHandler(
                NullLogger<AvatarCreateHandler>.Instance,
                characterRepository
            );
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
}
