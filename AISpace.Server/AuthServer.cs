using AISpace.Common;
using AISpace.Common.Game;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Server;

public class AuthServer(ILogger<AuthServer> logger, GameServerContext ctx, int port) : GameServerBase<AuthServer>(logger, ctx, port)
{
    protected override ServerType ActiveServerType => ServerType.Auth;

    protected override async Task InitializeAsync(CancellationToken ct)
    {
        if (!await Db.Users.AnyAsync(ct))
            await UserRepo.AddAsync("testuser", "password");
    }

    protected override void OnTick(CancellationToken ct)
    {
        //Clear expired sessions
    }
}
