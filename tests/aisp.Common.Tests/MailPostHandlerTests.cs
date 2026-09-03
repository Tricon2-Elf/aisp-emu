using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Msg;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;
using Moq;

namespace aisp.Common.Tests;

public class MailPostHandlerTests
{
    [Fact]
    public async Task Post_NotifiesOnlineRecipientWithInboxMail()
    {
        var state = new SharedState();
        var senderChar = new Character { Id = 1, Name = "Alice" };
        var recipientChar = new Character { Id = 2, Name = "Bob" };

        var sender = new CapturingPlayerSession
        {
            User = CreateUser(10),
            UserId = 10,
            CharacterId = 1,
            Character = senderChar,
        };
        var recipient = new CapturingPlayerSession
        {
            User = CreateUser(20),
            UserId = 20,
            CharacterId = 2,
            Character = recipientChar,
        };

        state.RegisterClient(ServerType.Msg, sender);
        state.RegisterClient(ServerType.Msg, recipient);

        var characters = new Mock<ICharacterRepository>();
        characters
            .Setup(c => c.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipientChar);
        characters
            .Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(senderChar);

        var handler = new MailPostHandler(characters.Object, state, WordFilter.FromTerms([]));
        var response = await handler.HandleAsync(
            new MailPostRequest(2, string.Empty, "[無題]", "hello"),
            sender,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(response);
        Assert.Equal(0u, response!.Result);
        Assert.Equal(1u, response.Mail.Type);
        Assert.Equal(1u, response.Mail.SenderId);
        Assert.Equal("Alice", response.Mail.SenderName);
        Assert.Equal(2u, response.Mail.DistId);
        Assert.Equal("Bob", response.Mail.DistName);

        var notify = recipient.Sent.Single(packet => packet.Type == PacketType.NotifyNewMail);
        var mail = MailData.FromBytes(notify.Payload);
        Assert.Equal(0u, mail.Type);
        Assert.Equal("Alice", mail.SenderName);
        Assert.Equal("Bob", mail.DistName);
        Assert.Equal("[無題]", mail.Subject);
        Assert.Equal("hello", mail.Body);
        Assert.Equal(response.Mail.MailId, mail.MailId);

        Assert.DoesNotContain(sender.Sent, packet => packet.Type == PacketType.NotifyNewMail);
    }

    [Fact]
    public async Task Post_LoadsSenderNameFromRepository_WhenSessionCharacterUnset()
    {
        var state = new SharedState();
        var senderChar = new Character { Id = 1, Name = "Alice" };
        var recipientChar = new Character { Id = 2, Name = "Bob" };

        var sender = new CapturingPlayerSession
        {
            User = CreateUser(10),
            UserId = 10,
            CharacterId = 1,
            Character = null,
        };
        var recipient = new CapturingPlayerSession
        {
            User = CreateUser(20),
            UserId = 20,
            CharacterId = 2,
        };

        state.RegisterClient(ServerType.Msg, sender);
        state.RegisterClient(ServerType.Msg, recipient);

        var characters = new Mock<ICharacterRepository>();
        characters
            .Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(senderChar);
        characters
            .Setup(c => c.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipientChar);

        var handler = new MailPostHandler(characters.Object, state, WordFilter.FromTerms([]));
        var response = await handler.HandleAsync(
            new MailPostRequest(2, string.Empty, "subj", "body"),
            sender,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0u, response!.Result);
        Assert.Equal("Alice", response.Mail.SenderName);

        var notify = recipient.Sent.Single(packet => packet.Type == PacketType.NotifyNewMail);
        Assert.Equal("Alice", MailData.FromBytes(notify.Payload).SenderName);
    }

    [Fact]
    public async Task Post_ReturnsFailure_WhenRecipientMissing()
    {
        var characters = new Mock<ICharacterRepository>();
        characters
            .Setup(c => c.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Character?)null);

        var handler = new MailPostHandler(
            characters.Object,
            new SharedState(),
            WordFilter.FromTerms([])
        );
        var sender = new CapturingPlayerSession
        {
            User = CreateUser(1),
            CharacterId = 1,
            Character = new Character { Id = 1, Name = "Alice" },
        };

        var response = await handler.HandleAsync(
            new MailPostRequest(99, string.Empty, "s", "b"),
            sender,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(response);
        Assert.Equal(1u, response!.Result);
    }

    [Fact]
    public async Task Post_RejectsBlockedSubjectOrBody()
    {
        var state = new SharedState();
        var senderChar = new Character { Id = 1, Name = "Alice" };
        var recipientChar = new Character { Id = 2, Name = "Bob" };
        var sender = new CapturingPlayerSession
        {
            User = CreateUser(10),
            UserId = 10,
            CharacterId = 1,
            Character = senderChar,
        };
        var recipient = new CapturingPlayerSession
        {
            User = CreateUser(20),
            UserId = 20,
            CharacterId = 2,
            Character = recipientChar,
        };
        state.RegisterClient(ServerType.Msg, sender);
        state.RegisterClient(ServerType.Msg, recipient);

        var characters = new Mock<ICharacterRepository>();
        characters
            .Setup(c => c.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipientChar);

        var handler = new MailPostHandler(
            characters.Object,
            state,
            WordFilter.FromTerms(["faggot"])
        );
        var response = await handler.HandleAsync(
            new MailPostRequest(2, string.Empty, "Faggot", "hello"),
            sender,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1u, response!.Result);
        Assert.DoesNotContain(recipient.Sent, packet => packet.Type == PacketType.NotifyNewMail);

        var bodyResponse = await handler.HandleAsync(
            new MailPostRequest(2, string.Empty, "hi", "you faggot"),
            sender,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(1u, bodyResponse!.Result);
        Assert.DoesNotContain(recipient.Sent, packet => packet.Type == PacketType.NotifyNewMail);
    }

    private static User CreateUser(int id)
    {
        var user = new User { Id = id, Username = $"user-{id}" };
        user.SetPassword("pw");
        return user;
    }
}
