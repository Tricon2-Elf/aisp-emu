using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class AvatarGetCreateInfoHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AvatarGetCreateInfoRequest;

    public PacketType ResponseType => PacketType.AvatarGetCreateInfoResponse;

    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        AvatarGetCreateInfoResponse resp = new();
        await session.SendAsync(ResponseType, resp.ToBytes(), ct);
    }
}
