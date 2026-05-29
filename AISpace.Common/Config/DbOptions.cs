using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Config;

public sealed class DbOptions
{
    public const string DefaultConnectionString = "Data Source=main.db";

    public string Provider { get; set; } = "sqlite";
    public string ConnectionString { get; set; } = DefaultConnectionString;

    public string EffectiveConnectionString => string.IsNullOrWhiteSpace(ConnectionString) ? DefaultConnectionString : ConnectionString;

    public void EnsureDataDirectoryExists()
    {
        var dataSource = new SqliteConnectionStringBuilder(EffectiveConnectionString).DataSource;
        if (string.IsNullOrEmpty(dataSource) || dataSource == ":memory:")
            return;

        var dir = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    public void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder)
    {
        if (!Provider.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Database provider '{Provider}' is not supported.");

        optionsBuilder.UseSqlite(EffectiveConnectionString, b => b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }
}
