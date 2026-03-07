using AISpace.Common;
using AISpace.Common.Game;

namespace AISpace.Server;

public class AuthServer(ILogger<AuthServer> logger, MainContext db, IUserRepository userRepo, AuthChannel channel, IWorldRepository worldRepo, PacketDispatcher dispatcher, SharedState state) : DomainServerBase<AuthServer>(logger, db, userRepo, channel.Channel, worldRepo, dispatcher, state)
{
    protected override MessageDomain ActiveDomain => MessageDomain.Auth;

    protected override void Initialize()
    {
        if (Db.Users.Any() == false)
            UserRepo.AddAsync("testuser", "password");
    }

    protected override void OnTick(CancellationToken ct)
    {
        //Clear expired sessions
    }
}
