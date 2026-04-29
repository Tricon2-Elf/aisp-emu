using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaUccAdvFigureBaseListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.UccAdvFigureBaseListRequest;

    public PacketType ResponseType => PacketType.UccAdvFigureBaseListResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new UccAdvFigureBaseListResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
