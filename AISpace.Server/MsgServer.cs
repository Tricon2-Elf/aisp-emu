using AISpace.Common.Game;

namespace AISpace.Server;

public class MsgServer(ILogger<MsgServer> logger, MainContext db, IUserRepository userRepo, MsgChannel channel, IWorldRepository worldRepo, PacketDispatcher dispatcher, SharedState state) : DomainServerBase<MsgServer>(logger, db, userRepo, channel.Channel, worldRepo, dispatcher, state)
{
    protected override MessageDomain ActiveDomain => MessageDomain.Msg;

    protected override void OnTick(CancellationToken ct)
    {
        while (!State.newMessages.IsEmpty)
        {
            State.newMessages.TryDequeue(out var message);
            Logger.LogInformation("{id} sent {message}", message.id, message.message);
            // Send message to all other users
        }
    }
}
