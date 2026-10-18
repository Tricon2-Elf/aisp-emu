using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaBattleEscapeHandler(ILogger<AreaBattleEscapeHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    // Unnamed client opcode used when leaving TPS mode via Escape.
    public PacketType RequestType => (PacketType)44845;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogInformation(
            "Character {Id} pressed Escape in TPS mode. Opening confirmation dialog.",
            session.CharacterId
        );

        if (session.IsTpsMode)
        {
            await session.SendAsync(
                PacketType.NotifyMissionOutmapChoiceOpen,
                new NotifyMissionOutmapChoiceOpen().ToBytes(),
                ct
            );
        }
    }
}
