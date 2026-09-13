using aisp.Common.Config;
using Microsoft.Data.Sqlite;

namespace aisp.Common.Services.Toxicity;

public static class ChatToxicityPaths
{
    public static string ResolveModelRoot(ChatToxicityOptions opts, DbOptions db)
    {
        ArgumentNullException.ThrowIfNull(opts);
        ArgumentNullException.ThrowIfNull(db);

        if (!string.IsNullOrWhiteSpace(opts.ModelRoot))
            return Path.GetFullPath(opts.ModelRoot);

        var dataSource = new SqliteConnectionStringBuilder(db.EffectiveConnectionString).DataSource;
        if (string.IsNullOrEmpty(dataSource) || dataSource == ":memory:")
            return Path.GetFullPath("models");

        var dir = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (string.IsNullOrEmpty(dir))
            return Path.GetFullPath("models");
        return Path.Combine(dir, "models");
    }
}
