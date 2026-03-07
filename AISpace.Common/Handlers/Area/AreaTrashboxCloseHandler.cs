using AISpace.Network.Packets.Area;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaTrashboxCloseHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.TrashboxCloseRequest;
    public PacketType ResponseType => PacketType.TrashboxCloseResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new TrashboxCloseResponse(1);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
