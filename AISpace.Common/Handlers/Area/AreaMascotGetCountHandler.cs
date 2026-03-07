using AISpace.Network.Packets.Area;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaMascotGetCountHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MascotGetCountRequest;

    public PacketType ResponseType => PacketType.MascotGetCountResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new MascotGetCountResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
