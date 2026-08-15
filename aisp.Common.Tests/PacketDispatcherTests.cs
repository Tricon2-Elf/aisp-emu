using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace aisp.Common.Tests;

public class PacketDispatcherTests
{
    private sealed class HandlerInvocationSink
    {
        public IPlayerSession? LastSession { get; set; }

        public int InvokeCount { get; set; }
    }

    private sealed class RecordingAuthAuthenticateHandler(HandlerInvocationSink sink)
        : IPacketHandler
    {
        public PacketType RequestType => PacketType.AuthenticateRequest;

        public PacketType ResponseType => PacketType.AuthenticateResponse;

        public ServerType ServerType => ServerType.Auth;

        public Task HandleAsync(
            ReadOnlyMemory<byte> payload,
            IPlayerSession session,
            CancellationToken ct = default
        )
        {
            sink.InvokeCount++;
            sink.LastSession = session;
            return Task.CompletedTask;
        }
    }

    private sealed class AuthPingOnlyHandler(HandlerInvocationSink sink) : IPacketHandler
    {
        public PacketType RequestType => PacketType.Ping;

        public PacketType ResponseType => PacketType.Ping;

        public ServerType ServerType => ServerType.Auth;

        public Task HandleAsync(
            ReadOnlyMemory<byte> payload,
            IPlayerSession session,
            CancellationToken ct = default
        )
        {
            sink.InvokeCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class AuthRequiredPingHandler(HandlerInvocationSink sink)
        : IPacketHandler,
            IRequiresAuthenticatedSession
    {
        public PacketType RequestType => PacketType.Ping;

        public PacketType ResponseType => PacketType.Ping;

        public ServerType ServerType => ServerType.Auth;

        public Task HandleAsync(
            ReadOnlyMemory<byte> payload,
            IPlayerSession session,
            CancellationToken ct = default
        )
        {
            sink.InvokeCount++;
            sink.LastSession = session;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task DispatchAsync_InvokesMatchingHandler()
    {
        var sink = new HandlerInvocationSink();
        var services = new ServiceCollection();
        services.AddSingleton(sink);
        services.AddScoped<IPacketHandler, RecordingAuthAuthenticateHandler>();
        services.AddScoped<RecordingAuthAuthenticateHandler>();
        services.AddSingleton<ILogger<PacketDispatcher>>(NullLogger<PacketDispatcher>.Instance);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<PacketDispatcher>();

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<PacketDispatcher>();
        var session = new CapturingPlayerSession();

        await dispatcher.DispatchAsync(
            ServerType.Auth,
            PacketType.AuthenticateRequest,
            [],
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, sink.InvokeCount);
        Assert.Same(session, sink.LastSession);
    }

    [Fact]
    public async Task DispatchAsync_DoesNotInvokeHandler_WhenRequestTypeMismatch()
    {
        var sink = new HandlerInvocationSink();
        var services = new ServiceCollection();
        services.AddSingleton(sink);
        services.AddScoped<IPacketHandler, AuthPingOnlyHandler>();
        services.AddScoped<AuthPingOnlyHandler>();
        services.AddSingleton<ILogger<PacketDispatcher>>(NullLogger<PacketDispatcher>.Instance);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<PacketDispatcher>();

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<PacketDispatcher>();

        await dispatcher.DispatchAsync(
            ServerType.Auth,
            PacketType.AuthenticateRequest,
            [],
            new CapturingPlayerSession(),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, sink.InvokeCount);
    }

    [Fact]
    public async Task DispatchAsync_DoesNotInvokeAuthRequiredHandler_WhenSessionIsUnauthenticated()
    {
        var sink = new HandlerInvocationSink();
        var services = new ServiceCollection();
        services.AddSingleton(sink);
        services.AddScoped<IPacketHandler, AuthRequiredPingHandler>();
        services.AddScoped<AuthRequiredPingHandler>();
        services.AddSingleton<ILogger<PacketDispatcher>>(NullLogger<PacketDispatcher>.Instance);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<PacketDispatcher>();

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<PacketDispatcher>();

        await dispatcher.DispatchAsync(
            ServerType.Auth,
            PacketType.Ping,
            [],
            new CapturingPlayerSession(),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, sink.InvokeCount);
    }

    [Fact]
    public async Task DispatchAsync_InvokesAuthRequiredHandler_WhenSessionIsAuthenticated()
    {
        var sink = new HandlerInvocationSink();
        var services = new ServiceCollection();
        services.AddSingleton(sink);
        services.AddScoped<IPacketHandler, AuthRequiredPingHandler>();
        services.AddScoped<AuthRequiredPingHandler>();
        services.AddSingleton<ILogger<PacketDispatcher>>(NullLogger<PacketDispatcher>.Instance);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<PacketDispatcher>();

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<PacketDispatcher>();
        var session = new CapturingPlayerSession
        {
            User = new User { Id = 1, Username = "test" },
        };

        await dispatcher.DispatchAsync(
            ServerType.Auth,
            PacketType.Ping,
            [],
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, sink.InvokeCount);
        Assert.Same(session, sink.LastSession);
    }
}
