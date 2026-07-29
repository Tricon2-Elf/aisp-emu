using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboAiscriptEndHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboAiscriptEndRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        _ = RoboAiscriptEndRequest.FromBytes(payload.Span);
        return Task.CompletedTask;
    }
}
