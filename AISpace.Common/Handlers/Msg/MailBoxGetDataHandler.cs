using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class MailBoxGetDataHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MailBoxGetDataRequest;

    public PacketType ResponseType => PacketType.MailBoxGetDataResponse;

    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        await session.SendAsync(ResponseType, new MailBoxGetDataResponse(0, 0).ToBytes(), ct);
    }
}
