using aisp.Common;
using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace aisp.Server;

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
