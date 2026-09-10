using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaMonsterGetDataHandler(ILogger<AreaMonsterGetDataHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MonsterGetDataRequest;
    public PacketType ResponseType => PacketType.MonsterGetDataResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogInformation(
            "TPS: Client requested monster data. Returning deferred spawn result."
        );
        await session.SendAsync(ResponseType, new MonsterGetDataResponse(100).ToBytes(), ct);
    }
}
