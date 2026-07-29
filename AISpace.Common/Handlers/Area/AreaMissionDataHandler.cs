using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaMissionDataHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MissionDataRequest;

    public PacketType ResponseType => PacketType.MissionDataResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new MissionDataResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
