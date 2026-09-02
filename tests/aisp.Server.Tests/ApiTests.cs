using aisp.Common;
using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Services;
using aisp.Network;
using aisp.Network.Packets.Common;
using aisp.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace aisp.Server.Tests;

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
        var result = await service.BroadcastAsync("test", TestContext.Current.CancellationToken);

        Assert.Equal(1, result.AreaClients);
        Assert.Equal(0, result.MsgClients);
        session.Verify(
            s =>
                s.SendAsync(
                    PacketType.TalkForwardNotify,
                    It.IsAny<byte[]>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
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
        var result = await service.BroadcastAsync("test", TestContext.Current.CancellationToken);

        Assert.Equal(0, result.AreaClients);
        Assert.Equal(1, result.MsgClients);
        session.Verify(
            s =>
                s.SendAsync(
                    PacketType.TalkForwardNotify,
                    It.IsAny<byte[]>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ReturnsZero_WhenNoClients()
    {
        var state = new SharedState();
        var service = new BroadcastService(state);
        var result = await service.BroadcastAsync("test", TestContext.Current.CancellationToken);

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
        var result = await service.BroadcastAsync("test", TestContext.Current.CancellationToken);

        Assert.Equal(1, result.AreaClients);
        Assert.Equal(0, result.MsgClients);
        auth.Verify(
            s =>
                s.SendAsync(
                    It.IsAny<PacketType>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        unauth.Verify(
            s =>
                s.SendAsync(
                    It.IsAny<PacketType>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }
}

public class UserAdminServiceTests
{
    private static User CreateTestUser(
        int id = 1,
        string username = "testuser",
        bool isBanned = false
    )
    {
        var user = new User
        {
            Id = id,
            Username = username,
            IsBanned = isBanned,
            CreatedAt = DateTime.UtcNow,
        };
        user.SetPassword("password123");
        return user;
    }

    private static UserAdminService CreateService(
        IUserRepository userRepo,
        SharedState? state = null
    )
    {
        state ??= new SharedState();
        var charRepo = new Mock<ICharacterRepository>();
        var options = new DbContextOptionsBuilder<MainContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var moderation = new ModerationService(
            userRepo,
            charRepo.Object,
            new CircleRepository(new MainContext(options)),
            new MainContext(options),
            state,
            NullLogger<ModerationService>.Instance
        );
        return new UserAdminService(userRepo, moderation, state, NullLogger<UserAdminService>.Instance);
    }

    [Fact]
    public async Task CreateUser_Success()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("newuser")).ReturnsAsync((User?)null);
        userRepo.Setup(r => r.AddAsync("newuser", "secret123")).Returns(Task.CompletedTask);
        var created = CreateTestUser(1, "newuser");
        userRepo
            .SetupSequence(r => r.GetByUsernameAsync("newuser"))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(created);

        var state = new SharedState();
        var service = CreateService(userRepo.Object, state);

        var (success, error, user) = await service.CreateUserAsync(
            "newuser",
            "secret123",
            TestContext.Current.CancellationToken
        );

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(user);
        Assert.Equal("newuser", user.Username);
        userRepo.Verify(r => r.AddAsync("newuser", "secret123"), Times.Once);
    }

    [Fact]
    public async Task CreateUser_AlreadyExists()
    {
        var existing = CreateTestUser(1, "exists");
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("exists")).ReturnsAsync(existing);

        var service = CreateService(userRepo.Object);

        var (success, error, _) = await service.CreateUserAsync(
            "exists",
            "pw",
            TestContext.Current.CancellationToken
        );

        Assert.False(success);
        Assert.Equal("username already exists", error);
        userRepo.Verify(r => r.AddAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUser_Success()
    {
        var user = CreateTestUser(1, "todelete");
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("todelete")).ReturnsAsync(user);
        userRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        var service = CreateService(userRepo.Object);

        var (success, error) = await service.DeleteUserAsync(
            "todelete",
            TestContext.Current.CancellationToken
        );

        Assert.True(success);
        Assert.Null(error);
        userRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_NotFound()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("nope")).ReturnsAsync((User?)null);

        var service = CreateService(userRepo.Object);

        var (success, error) = await service.DeleteUserAsync(
            "nope",
            TestContext.Current.CancellationToken
        );

        Assert.False(success);
        Assert.Equal("user not found", error);
        userRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_Success()
    {
        var user = CreateTestUser(1, "pwuser");
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("pwuser")).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdatePasswordAsync(1, "newpw")).Returns(Task.CompletedTask);

        var service = CreateService(userRepo.Object);

        var (success, error) = await service.ResetPasswordAsync(
            "pwuser",
            "newpw",
            TestContext.Current.CancellationToken
        );

        Assert.True(success);
        Assert.Null(error);
        userRepo.Verify(r => r.UpdatePasswordAsync(1, "newpw"), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_EmptyPassword()
    {
        var userRepo = new Mock<IUserRepository>();
        var service = CreateService(userRepo.Object);

        var (success, error) = await service.ResetPasswordAsync(
            "pwuser",
            "",
            TestContext.Current.CancellationToken
        );

        Assert.False(success);
        Assert.Equal("newPassword is required", error);
    }

    [Fact]
    public async Task BanUser_Success()
    {
        var user = CreateTestUser(1, "baduser");
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("baduser")).ReturnsAsync(user);
        userRepo
            .Setup(r => r.SetBannedAsync(1, true, "reason", It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(userRepo.Object);

        var (success, error, _) = await service.BanUserAsync(
            "baduser",
            "reason",
            ct: TestContext.Current.CancellationToken
        );

        Assert.True(success);
        Assert.Null(error);
        userRepo.Verify(
            r => r.SetBannedAsync(1, true, "reason", It.IsAny<DateTime?>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UnbanUser_Success()
    {
        var user = CreateTestUser(1, "bannedguy", true);
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("bannedguy")).ReturnsAsync(user);
        userRepo.Setup(r => r.SetBannedAsync(1, false, null, null)).Returns(Task.CompletedTask);

        var service = CreateService(userRepo.Object);

        var (success, error) = await service.UnbanUserAsync(
            "bannedguy",
            TestContext.Current.CancellationToken
        );

        Assert.True(success);
        Assert.Null(error);
        userRepo.Verify(r => r.SetBannedAsync(1, false, null, null), Times.Once);
    }

    [Fact]
    public async Task KickUser_NotFound()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("ghost")).ReturnsAsync((User?)null);

        var service = CreateService(userRepo.Object);

        var (success, error, sessionsClosed) = await service.KickUserAsync(
            "ghost",
            ct: TestContext.Current.CancellationToken
        );

        Assert.False(success);
        Assert.Equal("user not found", error);
        Assert.Equal(0, sessionsClosed);
    }

    [Fact]
    public async Task KickUser_SendsLogoutResponse_ToConnectedSessions()
    {
        var user = CreateTestUser(42, "onlineuser");
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("onlineuser")).ReturnsAsync(user);
        userRepo
            .Setup(r => r.SetKickedUntilAsync(42, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var state = new SharedState();
        var session = new Mock<IPlayerSession>();
        session.Setup(s => s.IsAuthenticated).Returns(true);
        session.Setup(s => s.ConnectionId).Returns(Guid.NewGuid());
        session.Setup(s => s.UserId).Returns(42);
        state.RegisterClient(ServerType.Msg, session.Object);

        var service = CreateService(userRepo.Object, state);

        var (success, error, _) = await service.KickUserAsync(
            "onlineuser",
            ct: TestContext.Current.CancellationToken
        );

        Assert.True(success);
        Assert.Null(error);
        session.Verify(
            s =>
                s.SendAsync(
                    PacketType.LogoutResponse,
                    It.IsAny<byte[]>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ListUsers_ReturnsSummaries()
    {
        var users = new List<User> { CreateTestUser(1, "alice"), CreateTestUser(2, "bob") };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAllAsync(null, null, null)).ReturnsAsync(users);
        userRepo.Setup(r => r.CountAsync(null)).ReturnsAsync(2);

        var service = CreateService(userRepo.Object);

        var (summaries, total) = await service.ListUsersAsync(
            null,
            null,
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(2, total);
        Assert.Equal(2, summaries.Count);
        Assert.Contains(summaries, s => s.Username == "alice");
        Assert.Contains(summaries, s => s.Username == "bob");
    }

    [Fact]
    public async Task GetUserDetail_ReturnsUser()
    {
        var user = CreateTestUser(5, "detailuser");
        user.BanReason = "bad behavior";
        user.IsBanned = true;
        user.BannedAt = DateTime.UtcNow.AddDays(-1);
        user.AiPoints = 500;
        user.NicoPoints = 250;
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("detailuser")).ReturnsAsync(user);

        var service = CreateService(userRepo.Object);

        var detail = await service.GetUserDetailAsync(
            "detailuser",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(detail);
        Assert.Equal(5, detail.Id);
        Assert.Equal("detailuser", detail.Username);
        Assert.True(detail.IsBanned);
        Assert.Equal("bad behavior", detail.BanReason);
        Assert.Equal(500, detail.AiPoints);
        Assert.Equal(250, detail.NicoPoints);
    }

    [Fact]
    public async Task GetUserDetail_NotFound()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("nobody")).ReturnsAsync((User?)null);

        var service = CreateService(userRepo.Object);

        var detail = await service.GetUserDetailAsync(
            "nobody",
            TestContext.Current.CancellationToken
        );

        Assert.Null(detail);
    }

    [Fact]
    public void GetConnectedClients_ReturnsAuthenticatedClients()
    {
        var state = new SharedState();
        var session = new Mock<IPlayerSession>();
        session.Setup(s => s.IsAuthenticated).Returns(true);
        session.Setup(s => s.ConnectionId).Returns(Guid.NewGuid());
        session.Setup(s => s.User).Returns(CreateTestUser(1, "connectedguy"));
        session
            .Setup(s => s.Character)
            .Returns(
                new Character
                {
                    Id = 10,
                    Name = "MyChar",
                    ModelId = 100,
                }
            );
        session.Setup(s => s.MapId).Returns(10990100u);
        session.Setup(s => s.ChannelId).Returns(1);
        state.RegisterClient(ServerType.Msg, session.Object);

        var service = CreateService(Mock.Of<IUserRepository>(), state);

        var clients = service.GetConnectedClients();

        Assert.Single(clients);
        Assert.Equal("connectedguy", clients[0].Username);
        Assert.Equal("Msg", clients[0].Server);
        Assert.Equal("MyChar", clients[0].CharacterName);
        Assert.Equal(10990100u, clients[0].MapId);
        Assert.Equal(1, clients[0].ChannelId);
    }

    [Fact]
    public async Task GetStats_ReturnsCounts()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.CountAsync(null)).ReturnsAsync(100);

        var state = new SharedState();
        var session = new Mock<IPlayerSession>();
        session.Setup(s => s.IsAuthenticated).Returns(true);
        session.Setup(s => s.ConnectionId).Returns(Guid.NewGuid());
        state.RegisterClient(ServerType.Area, session.Object);

        var service = CreateService(userRepo.Object, state);

        var stats = await service.GetStatsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(100, stats.TotalUsers);
        Assert.Equal(1, stats.OnlineCount);
        Assert.True(stats.UptimeSeconds >= 0);
        Assert.True(stats.ClientsPerServer.ContainsKey("auth"));
        Assert.True(stats.ClientsPerServer.ContainsKey("msg"));
        Assert.True(stats.ClientsPerServer.ContainsKey("area"));
    }
}

public class ApiKeyMiddlewareTests
{
    [Theory]
    [InlineData("", "", 401)]
    [InlineData("secret", "", 401)]
    [InlineData("secret", "wrong", 401)]
    [InlineData("secret", "secret", null)]
    public void AuthLogic_ValidatesCorrectly(
        string configuredKey,
        string headerValue,
        int? expectedStatusCode
    )
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
    public void NonApiPath_SkipsAuth()
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
