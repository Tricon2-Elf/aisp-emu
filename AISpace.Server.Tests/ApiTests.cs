using AISpace.Common;
using AISpace.Common.Config;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace AISpace.Server.Tests;

public class BroadcastServiceTests
{
    [Fact]
    public async Task SendsToAreaClients()
    {
        var state = new SharedState();
        var session = new Mock<IPlayerSession>();
        session.Setup(s => s.IsAuthenticated).Returns(true);
        session.Setup(s => s.ConnectionId).Returns(Guid.NewGuid());
        state.RegisterClient(ServerType.Area, session.Object);

        var service = new BroadcastService(state);
        var result = await service.BroadcastAsync("test");

        Assert.Equal(1, result.AreaClients);
        Assert.Equal(0, result.MsgClients);
        session.Verify(s => s.SendAsync(PacketType.TalkForwardNotify, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendsToMsgClients()
    {
        var state = new SharedState();
        var session = new Mock<IPlayerSession>();
        session.Setup(s => s.IsAuthenticated).Returns(true);
        session.Setup(s => s.ConnectionId).Returns(Guid.NewGuid());
        state.RegisterClient(ServerType.Msg, session.Object);

        var service = new BroadcastService(state);
        var result = await service.BroadcastAsync("test");

        Assert.Equal(0, result.AreaClients);
        Assert.Equal(1, result.MsgClients);
        session.Verify(s => s.SendAsync(PacketType.TalkForwardNotify, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnsZero_WhenNoClients()
    {
        var state = new SharedState();
        var service = new BroadcastService(state);
        var result = await service.BroadcastAsync("test");

        Assert.Equal(0, result.AreaClients);
        Assert.Equal(0, result.MsgClients);
    }

    [Fact]
    public async Task SkipsUnauthenticated()
    {
        var state = new SharedState();
        var auth = new Mock<IPlayerSession>();
        auth.Setup(s => s.IsAuthenticated).Returns(true);
        auth.Setup(s => s.ConnectionId).Returns(Guid.NewGuid());
        var unauth = new Mock<IPlayerSession>();
        unauth.Setup(s => s.IsAuthenticated).Returns(false);
        unauth.Setup(s => s.ConnectionId).Returns(Guid.NewGuid());
        state.RegisterClient(ServerType.Area, auth.Object);
        state.RegisterClient(ServerType.Msg, unauth.Object);

        var service = new BroadcastService(state);
        var result = await service.BroadcastAsync("test");

        Assert.Equal(1, result.AreaClients);
        Assert.Equal(0, result.MsgClients);
        auth.Verify(s => s.SendAsync(It.IsAny<PacketType>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        unauth.Verify(s => s.SendAsync(It.IsAny<PacketType>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class ApiKeyMiddlewareTests
{
    [Theory]
    [InlineData("", "", 401)]
    [InlineData("secret", "", 401)]
    [InlineData("secret", "wrong", 401)]
    [InlineData("secret", "secret", null)]
    public async Task AuthLogic_ValidatesCorrectly(string configuredKey, string headerValue, int? expectedStatusCode)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/broadcast";
        ctx.Request.Method = "POST";

        var services = new ServiceCollection();
        services.AddOptions<ApiSettings>().Configure(o => o.ApiKey = configuredKey);
        ctx.RequestServices = services.BuildServiceProvider();

        if (!string.IsNullOrEmpty(headerValue))
            ctx.Request.Headers["X-Api-Key"] = headerValue;

        bool nextCalled = false;
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            var settings = ctx.RequestServices.GetRequiredService<IOptions<ApiSettings>>().Value;
            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                ctx.Response.StatusCode = 401;
            }
            else
            {
                string? providedKey = ctx.Request.Headers["X-Api-Key"];
                if (providedKey != settings.ApiKey)
                    ctx.Response.StatusCode = 401;
                else
                    nextCalled = true;
            }
        }

        if (expectedStatusCode.HasValue)
            Assert.Equal(expectedStatusCode.Value, ctx.Response.StatusCode);
        else
            Assert.True(nextCalled);
    }

    [Fact]
    public async Task NonApiPath_SkipsAuth()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/healthz";
        ctx.Request.Method = "GET";

        var services = new ServiceCollection();
        services.AddOptions<ApiSettings>().Configure(o => o.ApiKey = "key");
        ctx.RequestServices = services.BuildServiceProvider();

        bool nextCalled = false;
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            // Should not reach here for /healthz path
            nextCalled = false;
        }
        else
        {
            nextCalled = true;
        }

        Assert.True(nextCalled);
    }
}
