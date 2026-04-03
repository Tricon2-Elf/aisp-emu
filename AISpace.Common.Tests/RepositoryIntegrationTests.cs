using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AISpace.Common.Tests;

public class RepositoryIntegrationTests
{
    [Fact]
    public async Task UserRepository_Add_GetByUsername_VerifyPassword()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var repo = new UserRepository(new MainContext(options));
            await repo.AddAsync("alice", "secret");
            var user = await repo.GetByUsernameAsync("alice");
            Assert.NotNull(user);
            Assert.True(user.VerifyPassword("secret"));
            Assert.False(user.VerifyPassword("wrong"));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task WorldRepository_Add_GetAll_GetById()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var repo = new WorldRepository(new MainContext(options));
            await repo.AddAsync("w1", "desc", "127.0.0.1", 50052);
            var all = await repo.GetAllAsync();
            Assert.Single(all);
            var id = all[0].Id;
            var w = await repo.GetByIdAsync(id);
            Assert.NotNull(w);
            Assert.Equal("w1", w!.Name);
            Assert.Equal("127.0.0.1", w.Address);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UserSessionRepository_Create_GetValidSession()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var userRepo = new UserRepository(new MainContext(options));
            await userRepo.AddAsync("bob", "pw");
            var user = await userRepo.GetByUsernameAsync("bob");
            Assert.NotNull(user);

            var db = new MainContext(options);
            var factory = new TestMainContextFactory(options);
            var sessionRepo = new UserSessionRepository(db, factory, NullLogger<UserSessionRepository>.Instance);
            const string otp = "1234567890123456";

            await sessionRepo.CreateAsync(user!.Id, otp, TimeSpan.FromHours(1), TestContext.Current.CancellationToken);

            var valid = await sessionRepo.GetValidSessionAsync(otp, TestContext.Current.CancellationToken);
            Assert.NotNull(valid);
            Assert.Equal(user.Id, valid!.UserId);
            Assert.NotNull(valid.User);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UserSessionRepository_GetValidSession_ReturnsNull_WhenExpired()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var userRepo = new UserRepository(new MainContext(options));
            await userRepo.AddAsync("carl", "pw");
            var user = await userRepo.GetByUsernameAsync("carl");

            var db = new MainContext(options);
            var factory = new TestMainContextFactory(options);
            var sessionRepo = new UserSessionRepository(db, factory, NullLogger<UserSessionRepository>.Instance);
            const string otp = "abcdefghijklmnop";

            await sessionRepo.CreateAsync(user!.Id, otp, TimeSpan.FromHours(1), TestContext.Current.CancellationToken);

            await using (var ctx = new MainContext(options))
            {
                var s = await ctx.UserSessions.SingleAsync(TestContext.Current.CancellationToken);
                s.ExpiresAt = DateTime.UtcNow.AddHours(-1);
                await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            Assert.Null(await sessionRepo.GetValidSessionAsync(otp, TestContext.Current.CancellationToken));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
