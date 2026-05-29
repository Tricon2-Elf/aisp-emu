using AISpace.Common.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AISpace.Common.DAL;

public sealed class MainContextFactory : IDesignTimeDbContextFactory<MainContext>
{
    public MainContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MainContext>();
        new DbOptions().ConfigureDbContext(optionsBuilder);
        return new MainContext(optionsBuilder.Options);
    }
}
