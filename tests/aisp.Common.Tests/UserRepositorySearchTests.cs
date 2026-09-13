using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Tests.Support;

namespace aisp.Common.Tests;

public sealed class UserRepositorySearchTests
{
    [Fact]
    public async Task GetAllAsync_MatchesCyrillicUsernameIgnoringCase()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);

        var user = new User { Username = "ИванИгрок" };
        user.SetPassword("pw");
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new UserRepository(db);
        var results = await repo.GetAllAsync("иванигрок");
        Assert.Equal(1, await repo.CountAsync("иванигрок"));
        Assert.Equal("ИванИгрок", Assert.Single(results).Username);
    }

    [Fact]
    public async Task GetAllAsync_MatchesCyrillicCharacterName()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);

        var user = new User { Username = "latin-user" };
        user.SetPassword("pw");
        user.Characters.Add(
            new Character
            {
                Name = "Александер",
                Birthdate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            }
        );
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new UserRepository(db);
        var results = await repo.GetAllAsync("алекс");
        Assert.Equal(1, await repo.CountAsync("алекс"));
        var found = Assert.Single(results);
        Assert.Equal("latin-user", found.Username);
        Assert.Contains(found.Characters, c => c.Name == "Александер");
    }

    [Fact]
    public async Task GetAllAsync_DoesNotMatchUnrelatedNames()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);

        var user = new User { Username = "bob" };
        user.SetPassword("pw");
        user.Characters.Add(
            new Character
            {
                Name = "Charlie",
                Birthdate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            }
        );
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new UserRepository(db);
        Assert.Empty(await repo.GetAllAsync("иван"));
        Assert.Equal(0, await repo.CountAsync("иван"));
    }
}
