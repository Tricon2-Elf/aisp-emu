using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Services;
using aisp.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class ModerationServiceTests
{
    [Fact]
    public async Task KickAsync_RejectsEqualRoleTarget()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var actor = CreateUser(1, "mod", UserRole.Moderator);
            var target = CreateUser(2, "other-mod", UserRole.Moderator);
            await SeedUsersAsync(options, actor, target);

            var service = CreateService(options);
            var (error, _) = await service.KickAsync(
                1,
                "other-mod",
                ct: TestContext.Current.CancellationToken
            );

            Assert.Equal(ModerationError.PermissionDenied, error);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task BanAsync_DefaultsToOneDay()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var actor = CreateUser(1, "admin", UserRole.Admin);
            var target = CreateUser(2, "player", UserRole.User);
            await SeedUsersAsync(options, actor, target);

            var service = CreateService(options);
            var (error, _) = await service.BanAsync(1, "player", ct: TestContext.Current.CancellationToken);

            Assert.Equal(ModerationError.None, error);
            await using var db = new MainContext(options);
            var saved = await db.Users.SingleAsync(u => u.Id == 2);
            Assert.True(saved.IsBanned);
            Assert.NotNull(saved.BannedUntil);
            Assert.InRange(
                saved.BannedUntil!.Value,
                DateTime.UtcNow.AddHours(23),
                DateTime.UtcNow.AddDays(1).AddMinutes(1)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PromoteToModeratorAsync_TogglesUserRole()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var actor = CreateUser(1, "admin", UserRole.Admin);
            var target = CreateUser(2, "player", UserRole.User);
            await SeedUsersAsync(options, actor, target);

            var service = CreateService(options);
            Assert.Equal(
                ModerationError.None,
                await service.PromoteToModeratorAsync(1, "player", TestContext.Current.CancellationToken)
            );
            Assert.Equal(
                ModerationError.None,
                await service.DemoteFromModeratorAsync(1, "player", TestContext.Current.CancellationToken)
            );

            await using var db = new MainContext(options);
            Assert.Equal(UserRole.User, (await db.Users.SingleAsync(u => u.Id == 2)).Role);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(20, 15)]
    public void ClampKickMinutes_ClampsToValidRange(int input, int expected) =>
        Assert.Equal(expected, ModerationService.ClampKickMinutes(input));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(45, 30)]
    public void ClampModeratorBanDays_ClampsToValidRange(int input, int expected) =>
        Assert.Equal(expected, ModerationService.ClampModeratorBanDays(input));

    [Fact]
    public void ResolveBanDuration_ModeratorCannotPermaban()
    {
        var result = ModerationService.ResolveBanDuration(UserRole.Moderator, 0);
        Assert.Equal(ModerationError.InvalidDuration, result.Error);
    }

    [Fact]
    public void ResolveBanDuration_AdminCanPermaban()
    {
        var result = ModerationService.ResolveBanDuration(UserRole.Admin, 0);
        Assert.Equal(ModerationError.None, result.Error);
        Assert.True(result.IsPermanent);
    }

    [Fact]
    public void ResolveBanDuration_ModeratorCapsAtThirtyDays()
    {
        var result = ModerationService.ResolveBanDuration(UserRole.Moderator, 100);
        Assert.Equal(ModerationError.None, result.Error);
        Assert.InRange(
            result.BannedUntil!.Value,
            DateTime.UtcNow.AddDays(29),
            DateTime.UtcNow.AddDays(31)
        );
    }

    [Fact]
    public async Task BanAsync_AdminPermanentBan()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var actor = CreateUser(1, "admin", UserRole.Admin);
            var target = CreateUser(2, "player", UserRole.User);
            await SeedUsersAsync(options, actor, target);

            var service = CreateService(options);
            var (error, _) = await service.BanAsync(
                1,
                "player",
                0,
                ct: TestContext.Current.CancellationToken
            );

            Assert.Equal(ModerationError.None, error);
            await using var db = new MainContext(options);
            var saved = await db.Users.SingleAsync(u => u.Id == 2);
            Assert.True(saved.IsBanned);
            Assert.Null(saved.BannedUntil);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ResetPasswordAsync_AllowsModeratorToResetUserPassword()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var actor = CreateUser(1, "mod", UserRole.Moderator);
            var target = CreateUser(2, "player", UserRole.User);
            await SeedUsersAsync(options, actor, target);

            var service = CreateService(options);
            var error = await service.ResetPasswordAsync(
                1,
                2,
                "new-password-123",
                TestContext.Current.CancellationToken
            );

            Assert.Equal(ModerationError.None, error);
            await using var db = new MainContext(options);
            var saved = await db.Users.SingleAsync(u => u.Id == 2);
            Assert.True(saved.VerifyPassword("new-password-123"));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ResetPasswordAsync_RejectsEqualRoleTarget()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var actor = CreateUser(1, "mod", UserRole.Moderator);
            var target = CreateUser(2, "other-mod", UserRole.Moderator);
            await SeedUsersAsync(options, actor, target);

            var service = CreateService(options);
            var error = await service.ResetPasswordAsync(
                1,
                2,
                "new-password-123",
                TestContext.Current.CancellationToken
            );

            Assert.Equal(ModerationError.PermissionDenied, error);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static User CreateUser(int id, string username, UserRole role)
    {
        var user = new User { Id = id, Username = username, Role = role };
        user.SetPassword("pw");
        return user;
    }

    private static async Task SeedUsersAsync(
        DbContextOptions<MainContext> options,
        params User[] users
    )
    {
        await using var db = new MainContext(options);
        db.Users.AddRange(users);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static ModerationService CreateService(DbContextOptions<MainContext> options)
    {
        var state = new SharedState();
        return new ModerationService(
            new UserRepository(new MainContext(options)),
            new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance),
            state,
            NullLogger<ModerationService>.Instance
        );
    }
}
