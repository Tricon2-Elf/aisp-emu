using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboSquireHandler(IRoboRepository roboRepository, ILogger<AreaRoboSquireHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboSquireRequest;
    public PacketType ResponseType => PacketType.RoboSquireResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var characterId = checked((int)session.CharacterId);
        var request = RoboSquireRequest.FromBytes(payload.Span);
        var robo = await roboRepository.GetAsync(characterId, request.RoboId, ct);
        if (robo is null)
        {
            logger.LogWarning("Character {CharacterId} tried to take unowned Robo {RoboId} outside", session.CharacterId, request.RoboId);
            await session.SendAsync(ResponseType, new RoboSquireResponse(request.RoboId, 1).ToBytes(), ct);
            return;
        }

        await roboRepository.UpdateStateAsync(characterId, request.RoboId, (uint)RoboState.Accompanying, ct);

        var map = new CharacterMapData
        {
            ChannelId = checked((uint)session.ChannelId),
            MapId = session.MapId,
            Movement = new MovementData(session.X, session.Y, session.Z, session.Rotation, MovementType.Stopped),
        };

        // The response callback immediately activates the companion from cached RoboData.
        // Update that cache first so it observes state 2 (accompanying).
        var stateUpdate = new NotifyUpdateRoboState(request.RoboId, robo.Character.SlotId, (uint)RoboState.Accompanying, map);
        await session.SendAsync(PacketType.NotifyUpdateRoboState, stateUpdate.ToBytes(), ct);
        await session.SendAsync(ResponseType, new RoboSquireResponse(request.RoboId, 0).ToBytes(), ct);

        logger.LogInformation("Robo {RoboId} now accompanies character {CharacterId}", request.RoboId, session.CharacterId);
    }
}
