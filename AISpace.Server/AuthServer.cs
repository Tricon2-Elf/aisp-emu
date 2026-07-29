using AISpace.Common;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AISpace.Server;

public class AuthServer(ILogger<AuthServer> logger, GameServerContext ctx, int port)
    : GameServerBase<AuthServer>(logger, ctx, port)
{
    protected override ServerType ActiveServerType => ServerType.Auth;

    protected override async Task InitializeAsync(CancellationToken ct)
    {
        using var scope = ScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainContext>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        if (!await db.Users.AnyAsync(ct))
            await userRepo.AddAsync("testuser", "password");
    }
}
