using System.Buffers.Binary;
using System.Text;
using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Auth;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Packets.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AISpace.Common.Tests;

public class AuthenticateHandlerTests
{
    [Fact]
    public async Task UnknownUser_SendsFailure_DoesNotCreateAccount()
    {
        var userRepo = new Mock<AISpace.Common.DAL.Repositories.IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("newbie")).ReturnsAsync((User?)null);

        var handler = new AuthenticateHandler(
            userRepo.Object,
            new SharedState(),
            NullLogger<AuthenticateHandler>.Instance
        );
        var session = new CapturingPlayerSession();
        var req = new AuthenticateRequest("newbie", "pw");

        var resp = await handler.HandleAsync(req, session, TestContext.Current.CancellationToken);

        Assert.Null(resp);
        Assert.Null(session.User);
        userRepo.Verify(r => r.AddAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.Single(session.Sent);
        Assert.Equal(PacketType.AuthenticateFailureResponse, session.Sent[0].Type);
        Assert.Equal(
            (uint)AuthResponseResult.InvalidCredentials,
            BinaryPrimitives.ReadUInt32LittleEndian(session.Sent[0].Payload.AsSpan(0, 4))
        );
    }

    [Fact]
    public async Task WrongPassword_SendsFailure_DoesNotSetUser()
    {
        var user = new User { Id = 1, Username = "bob" };
        user.SetPassword("right");

        var userRepo = new Mock<AISpace.Common.DAL.Repositories.IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("bob")).ReturnsAsync(user);

        var handler = new AuthenticateHandler(
            userRepo.Object,
            new SharedState(),
            NullLogger<AuthenticateHandler>.Instance
        );
        var session = new CapturingPlayerSession();
        var req = new AuthenticateRequest("bob", "wrong");

        var resp = await handler.HandleAsync(req, session, TestContext.Current.CancellationToken);

        Assert.Null(resp);
        Assert.Null(session.User);
        Assert.Single(session.Sent);
        Assert.Equal(PacketType.AuthenticateFailureResponse, session.Sent[0].Type);
        Assert.Equal(
            (uint)AuthResponseResult.InvalidCredentials,
            BinaryPrimitives.ReadUInt32LittleEndian(session.Sent[0].Payload.AsSpan(0, 4))
        );
    }

    [Fact]
    public async Task CorrectPassword_ReturnsSuccess()
    {
        var user = new User { Id = 3, Username = "alice" };
        user.SetPassword("ok");

        var userRepo = new Mock<AISpace.Common.DAL.Repositories.IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("alice")).ReturnsAsync(user);

        var state = new SharedState();
        var handler = new AuthenticateHandler(
            userRepo.Object,
            state,
            NullLogger<AuthenticateHandler>.Instance
        );
        IPacketHandler wire = handler;
        var session = new CapturingPlayerSession();
        var w = new PacketWriter();
        w.Write("alice");
        w.Write("ok");

        await wire.HandleAsync(w.ToBytes(), session, TestContext.Current.CancellationToken);

        Assert.Equal(3, session.UserId);
        Assert.Contains(state.AuthClients, client => client.ConnectionId == session.ConnectionId);
        Assert.Single(session.Sent);
        Assert.Equal(PacketType.AuthenticateResponse, session.Sent[0].Type);
        Assert.Equal(
            3u,
            BinaryPrimitives.ReadUInt32LittleEndian(session.Sent[0].Payload.AsSpan(0, 4))
        );
    }

    [Fact]
    public async Task BannedUser_SendsAccountBanned_DoesNotSetUser()
    {
        var user = new User
        {
            Id = 5,
            Username = "banned",
            IsBanned = true,
            BanReason = "cheating",
        };
        user.SetPassword("pw");

        var userRepo = new Mock<AISpace.Common.DAL.Repositories.IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("banned")).ReturnsAsync(user);

        var handler = new AuthenticateHandler(
            userRepo.Object,
            new SharedState(),
            NullLogger<AuthenticateHandler>.Instance
        );
        var session = new CapturingPlayerSession();
        var req = new AuthenticateRequest("banned", "pw");

        var resp = await handler.HandleAsync(req, session, TestContext.Current.CancellationToken);

        Assert.Null(resp);
        Assert.Null(session.User);
        Assert.Single(session.Sent);
        Assert.Equal(PacketType.AuthenticateFailureResponse, session.Sent[0].Type);
        Assert.Equal(
            (uint)AuthResponseResult.AccountBanned,
            BinaryPrimitives.ReadUInt32LittleEndian(session.Sent[0].Payload.AsSpan(0, 4))
        );
    }
}
