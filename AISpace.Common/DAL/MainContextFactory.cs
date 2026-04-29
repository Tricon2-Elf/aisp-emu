using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AISpace.Common.DAL;

public sealed class MainContextFactory : IDesignTimeDbContextFactory<MainContext>
{
    public MainContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite("Data Source=main.db", b => b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).Options;

        return new MainContext(options);
    }
}
