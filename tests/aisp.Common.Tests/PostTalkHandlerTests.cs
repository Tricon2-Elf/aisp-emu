using System.Buffers.Binary;
using System.Text;
using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Handlers.Msg;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Tests;

public class PostTalkHandlerTests
{
    [Fact]
    public async Task ForwardNotify_IncludesSenderCharacterId()
    {
        var user = CreateUser(1, 9001);
        var state = new SharedState();

        var sender = new CapturingPlayerSession
        {
            User = user,
            UserId = user.Id,
            CharacterId = 9001,
        };
        var recipient = new CapturingPlayerSession
        {
            User = CreateUser(2, 9002),
            UserId = 2,
            CharacterId = 9002,
        };

        state.RegisterClient(ServerType.Msg, sender);
        state.RegisterClient(ServerType.Msg, recipient);

        var handler = new PostTalkHandler(state);
        await handler.HandleAsync(
            BuildPostTalkPayload(1, -1, "hello", 0),
            sender,
            TestContext.Current.CancellationToken
        );

        var forward = recipient.Sent.Single(packet => packet.Type == PacketType.TalkForwardNotify);
        Assert.Equal(9001u, BinaryPrimitives.ReadUInt32LittleEndian(forward.Payload.AsSpan(0, 4)));
    }

    [Fact]
    public async Task ForwardNotify_FallsBackToAreaSessionCharacterId_WhenMsgCharacterIdUnset()
    {
        var user = CreateUser(1, 9001);
        var state = new SharedState();

        var areaSession = new CapturingPlayerSession
        {
            User = user,
            UserId = user.Id,
            CharacterId = 9001,
        };
        var sender = new CapturingPlayerSession { User = user, UserId = user.Id };
        var recipient = new CapturingPlayerSession
        {
            User = CreateUser(2, 9002),
            UserId = 2,
            CharacterId = 9002,
        };

        state.RegisterClient(ServerType.Area, areaSession);
        state.RegisterClient(ServerType.Msg, sender);
        state.RegisterClient(ServerType.Msg, recipient);

        var handler = new PostTalkHandler(state);
        await handler.HandleAsync(
            BuildPostTalkPayload(1, -1, "hello", 0),
            sender,
            TestContext.Current.CancellationToken
        );

        var forward = recipient.Sent.Single(packet => packet.Type == PacketType.TalkForwardNotify);
        Assert.Equal(9001u, BinaryPrimitives.ReadUInt32LittleEndian(forward.Payload.AsSpan(0, 4)));
    }

    private static User CreateUser(int userId, int characterId)
    {
        var user = new User { Id = userId, Username = $"user{userId}" };
        user.Characters.Add(
            new Character
            {
                Id = characterId,
                Name = $"char{characterId}",
                UserId = userId,
            }
        );
        return user;
    }

    private static byte[] BuildPostTalkPayload(
        uint messageId,
        int distId,
        string message,
        uint balloonId
    )
    {
        var writer = new PacketWriter();
        writer.Write(messageId);
        writer.Write((uint)distId);
        writer.Write(message, "Shift_JIS");
        writer.Write(balloonId);
        return writer.ToBytes();
    }
}
