using AISpace.Common.DAL;
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
}

internal sealed class TestMainContextFactory(DbContextOptions<MainContext> options) : IDbContextFactory<MainContext>
{
    public MainContext CreateDbContext() => new MainContext(options);
}
