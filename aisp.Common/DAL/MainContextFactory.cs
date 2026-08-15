using aisp.Common.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace aisp.Common.DAL;

public sealed class MainContextFactory : IDesignTimeDbContextFactory<MainContext>
{
    public MainContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MainContext>();
        new DbOptions().ConfigureDbContext(optionsBuilder);
        return new MainContext(optionsBuilder.Options);
    }
}
