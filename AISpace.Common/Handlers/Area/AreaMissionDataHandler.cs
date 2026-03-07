using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaMissionDataHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MissionDataRequest;

    public PacketType ResponseType => PacketType.MissionDataResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new MissionDataResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
