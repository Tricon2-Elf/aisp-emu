using aisp.Common.DAL;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public class MigrationTests
{
    [Fact]
    public void MainContext_HasNoPendingModelChanges()
    {
        using var db = new MainContextFactory().CreateDbContext([]);

        Assert.False(
            db.Database.HasPendingModelChanges(),
            "MainContext has pending model changes. Run ./scripts/generate-migration.sh <MigrationName>."
        );
    }
}
