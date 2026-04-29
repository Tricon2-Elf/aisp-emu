using AISpace.Common.Game;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AISpace.Common.Tests;

public class PacketDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_InvokesMatchingHandler()
    {
        var handlerMock = new Mock<IPacketHandler>();
        handlerMock.Setup(h => h.ServerType).Returns(ServerType.Auth);
        handlerMock.Setup(h => h.RequestType).Returns(PacketType.AuthenticateRequest);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<IPlayerSession>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

        var services = new ServiceCollection();
        services.AddSingleton<IPacketHandler>(handlerMock.Object);
        services.AddSingleton<ILogger<PacketDispatcher>>(NullLogger<PacketDispatcher>.Instance);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<PacketDispatcher>();

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<PacketDispatcher>();
        var session = new CapturingPlayerSession();

        await dispatcher.DispatchAsync(ServerType.Auth, PacketType.AuthenticateRequest, [], session, TestContext.Current.CancellationToken);

        handlerMock.Verify(h => h.HandleAsync(It.IsAny<ReadOnlyMemory<byte>>(), session, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_DoesNotInvokeHandler_WhenRequestTypeMismatch()
    {
        var handlerMock = new Mock<IPacketHandler>();
        handlerMock.Setup(h => h.ServerType).Returns(ServerType.Auth);
        handlerMock.Setup(h => h.RequestType).Returns(PacketType.Ping);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<IPlayerSession>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

        var services = new ServiceCollection();
        services.AddSingleton<IPacketHandler>(handlerMock.Object);
        services.AddSingleton<ILogger<PacketDispatcher>>(NullLogger<PacketDispatcher>.Instance);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<PacketDispatcher>();

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<PacketDispatcher>();

        await dispatcher.DispatchAsync(ServerType.Auth, PacketType.AuthenticateRequest, [], new CapturingPlayerSession(), TestContext.Current.CancellationToken);

        handlerMock.Verify(h => h.HandleAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<IPlayerSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
