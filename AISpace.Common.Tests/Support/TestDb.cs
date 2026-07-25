using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Tests.Support;

internal static class TestDb
{
    /// <summary>Opens an in-memory SQLite connection and builds a <see cref="MainContext"/> schema. Caller disposes the connection.</summary>
    public static (SqliteConnection Connection, DbContextOptions<MainContext> Options) CreateInMemoryMainContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite(connection).Options;
        using (var ctx = new MainContext(options))
        {
            ctx.Database.EnsureCreated();
        }
        return (connection, options);
    }

    public static async Task SeedCharacterAsync(DbContextOptions<MainContext> options, int characterId, CancellationToken ct = default)
    {
        await using var db = new MainContext(options);
        var user = new User { Id = characterId, Username = $"user-{characterId}" };
        user.SetPassword("pw");
        user.Characters.Add(
            new Character
            {
                Id = characterId,
                Name = $"character-{characterId}",
                UserId = characterId,
                Birthdate = new DateTime(2000, 1, 1),
            }
        );
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}

internal sealed class TestMainContextFactory(DbContextOptions<MainContext> options) : IDbContextFactory<MainContext>
{
    public MainContext CreateDbContext() => new(options);
}
