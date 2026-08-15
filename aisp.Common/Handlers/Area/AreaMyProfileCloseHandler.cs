using aisp.Common.Game;
using aisp.Network;

namespace aisp.Common.Handlers.Area;

public class AreaMyProfileCloseHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyProfileCloseRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        await Task.CompletedTask;
    }
}
