using AISpace.Network.Packets.Area;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreasvLeaveHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AreasvLeaveRequest;

    public PacketType ResponseType => PacketType.AreasvLeaveResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new AreasvLeaveResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
