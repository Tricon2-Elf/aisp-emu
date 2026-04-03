using System.Buffers.Binary;
using AISpace.Common.Config;
using AISpace.Common.DAL.Entities;
using AISpace.Common.Handlers.Auth;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AISpace.Common.Tests;

public class WorldSelectHandlerTests
{
    private static ServerOptions TestOptions() => new() { NetworkOptions = new NetworkOptions(), DbOptions = new DbOptions() };

    [Fact]
    public async Task NotAuthenticated_SendsError()
    {
        var worldRepo = new Mock<AISpace.Common.DAL.Repositories.IWorldRepository>();
        var sessionRepo = new Mock<AISpace.Common.DAL.Repositories.IUserSessionRepository>();
        var handler = new WorldSelectHandler(worldRepo.Object, sessionRepo.Object, NullLogger<WorldSelectHandler>.Instance, Options.Create(TestOptions()));
        var session = new CapturingPlayerSession { User = null };

        var w = new PacketWriter();
        w.Write(1u);
        await handler.HandleAsync(w.ToBytes(), session, TestContext.Current.CancellationToken);

        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(session.Sent[0].Payload.AsSpan(0, 4)));
        sessionRepo.Verify(r => r.CreateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnknownWorld_SendsError()
    {
        var worldRepo = new Mock<AISpace.Common.DAL.Repositories.IWorldRepository>();
        worldRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((World?)null);
        var sessionRepo = new Mock<AISpace.Common.DAL.Repositories.IUserSessionRepository>();
        var handler = new WorldSelectHandler(worldRepo.Object, sessionRepo.Object, NullLogger<WorldSelectHandler>.Instance, Options.Create(TestOptions()));
        var session = new CapturingPlayerSession
        {
            User = new User { Id = 1, Username = "x" },
        };

        var w = new PacketWriter();
        w.Write(999u);
        await handler.HandleAsync(w.ToBytes(), session, TestContext.Current.CancellationToken);

        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(session.Sent[0].Payload.AsSpan(0, 4)));
        sessionRepo.Verify(r => r.CreateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Success_CreatesSession_AndSendsResolvedAddress()
    {
        var world = new World
        {
            Id = 2,
            Name = "w",
            Description = "",
            Address = "localhost",
            Port = 50052,
        };
        var worldRepo = new Mock<AISpace.Common.DAL.Repositories.IWorldRepository>();
        worldRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(world);

        var sessionRepo = new Mock<AISpace.Common.DAL.Repositories.IUserSessionRepository>();
        sessionRepo
            .Setup(r => r.CreateAsync(1, It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new UserSession
                {
                    UserId = 1,
                    OTP = "x",
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                }
            );

        var opts = TestOptions();
        opts.IPOverride = "10.0.0.1";
        var handler = new WorldSelectHandler(worldRepo.Object, sessionRepo.Object, NullLogger<WorldSelectHandler>.Instance, Options.Create(opts));
        var session = new CapturingPlayerSession
        {
            User = new User { Id = 1, Username = "x" },
        };

        var w = new PacketWriter();
        w.Write(2u);
        await handler.HandleAsync(w.ToBytes(), session, TestContext.Current.CancellationToken);

        sessionRepo.Verify(r => r.CreateAsync(1, It.IsAny<string>(), TimeSpan.FromHours(1), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(session.Sent[0].Payload.AsSpan(0, 4)));
        var parsed = ParseWorldSelectResponse(session.Sent[0].Payload);
        Assert.Equal("10.0.0.1", parsed.Ip);
        Assert.Equal((ushort)50052, parsed.Port);
        Assert.Equal(20, parsed.Otp.Length);
    }

    private static (string Ip, ushort Port, string Otp) ParseWorldSelectResponse(byte[] payload)
    {
        var r = new PacketReader(payload);
        _ = r.ReadUInt();
        _ = r.ReadUInt();
        var port = r.ReadUShort();
        var ip = r.ReadFixedString(65, "ASCII").TrimEnd('\0');
        var otp = r.ReadFixedString(20, "ASCII").TrimEnd('\0');
        return (ip, port, otp);
    }
}
