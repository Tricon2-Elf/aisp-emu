using System.Buffers.Binary;
using System.Text;
using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Handlers.Msg;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace aisp.Common.Tests;

public class LoginHandlerTests
{
    [Fact]
    public async Task ValidOtp_Succeeds_AndRegistersMsgClient()
    {
        const string otp = "12345678901234567890";
        var user = new User { Id = 5, Username = "u" };
        user.Characters.Add(
            new Character
            {
                Id = 42,
                Name = "ChatUser",
                UserId = 5,
            }
        );
        var us = new UserSession
        {
            UserId = 5,
            OTP = otp,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        };

        var sessionRepo = new Mock<aisp.Common.DAL.Repositories.IUserSessionRepository>();
        sessionRepo
            .Setup(r => r.GetValidSessionAsync(otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(us);

        var state = new SharedState();
        var circles = new Mock<aisp.Common.DAL.Repositories.ICircleRepository>();
        circles
            .Setup(r =>
                r.GetMembershipsForCharacterAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Array.Empty<(aisp.Common.DAL.Entities.Circle, uint)>());
        var handler = new LoginHandler(
            sessionRepo.Object,
            circles.Object,
            state,
            NullLogger<LoginHandler>.Instance
        );
        IPacketHandler wire = handler;
        var session = new CapturingPlayerSession();

        var w = new PacketWriter();
        w.Write(5u);
        w.Write(Encoding.ASCII.GetBytes(otp));

        await wire.HandleAsync(w.ToBytes(), session, TestContext.Current.CancellationToken);

        Assert.Equal(5, session.UserId);
        Assert.NotNull(session.User);
        Assert.Equal(42u, session.CharacterId);
        Assert.Single(session.Sent);
        Assert.Equal(PacketType.LoginResponse, session.Sent[0].Type);
        Assert.Equal(
            (uint)AuthResponseResult.Success,
            BinaryPrimitives.ReadUInt32LittleEndian(session.Sent[0].Payload.AsSpan(0, 4))
        );
        Assert.Contains(state.MsgClients, client => client.ConnectionId == session.ConnectionId);
    }

    [Fact]
    public async Task InvalidOtp_ReturnsInvalidCredentials()
    {
        var sessionRepo = new Mock<aisp.Common.DAL.Repositories.IUserSessionRepository>();
        sessionRepo
            .Setup(r => r.GetValidSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession?)null);

        var circles = new Mock<aisp.Common.DAL.Repositories.ICircleRepository>();
        circles
            .Setup(r =>
                r.GetMembershipsForCharacterAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Array.Empty<(aisp.Common.DAL.Entities.Circle, uint)>());
        var handler = new LoginHandler(
            sessionRepo.Object,
            circles.Object,
            new SharedState(),
            NullLogger<LoginHandler>.Instance
        );
        IPacketHandler wire = handler;
        var session = new CapturingPlayerSession();

        var w = new PacketWriter();
        w.Write(1u);
        w.Write(Encoding.ASCII.GetBytes("00000000000000000000"));

        await wire.HandleAsync(w.ToBytes(), session, TestContext.Current.CancellationToken);

        Assert.Null(session.User);
        Assert.Equal(
            (uint)AuthResponseResult.InvalidCredentials,
            BinaryPrimitives.ReadUInt32LittleEndian(session.Sent[0].Payload.AsSpan(0, 4))
        );
    }

    [Fact]
    public async Task UserIdMismatch_ReturnsInvalidCredentials()
    {
        var user = new User { Id = 1, Username = "a" };
        var us = new UserSession
        {
            UserId = 1,
            OTP = "aaaaaaaaaaaaaaaaaaaa",
            User = user,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        };

        var sessionRepo = new Mock<aisp.Common.DAL.Repositories.IUserSessionRepository>();
        sessionRepo
            .Setup(r => r.GetValidSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(us);

        var circles = new Mock<aisp.Common.DAL.Repositories.ICircleRepository>();
        circles
            .Setup(r =>
                r.GetMembershipsForCharacterAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Array.Empty<(aisp.Common.DAL.Entities.Circle, uint)>());
        var handler = new LoginHandler(
            sessionRepo.Object,
            circles.Object,
            new SharedState(),
            NullLogger<LoginHandler>.Instance
        );
        IPacketHandler wire = handler;
        var session = new CapturingPlayerSession();

        var w = new PacketWriter();
        w.Write(99u);
        w.Write(Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaa"));

        await wire.HandleAsync(w.ToBytes(), session, TestContext.Current.CancellationToken);

        Assert.Equal(
            (uint)AuthResponseResult.InvalidCredentials,
            BinaryPrimitives.ReadUInt32LittleEndian(session.Sent[0].Payload.AsSpan(0, 4))
        );
    }
}
