using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboCallHandler(IRoboRepository roboRepository, ILogger<AreaRoboCallHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboCallRequest;
    public PacketType ResponseType => PacketType.RoboCallResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var characterId = checked((int)session.CharacterId);
        var request = RoboCallRequest.FromBytes(payload.Span);
        logger.LogInformation("RoboCallRequest from character {CharacterId}: roboId={RoboId}", session.CharacterId, request.RoboId);

        var robo = await roboRepository.GetAsync(characterId, request.RoboId, ct);
        if (robo is null)
        {
            logger.LogWarning("Character {CharacterId} tried to call unowned Robo {RoboId}", session.CharacterId, request.RoboId);
            await session.SendAsync(ResponseType, new RoboCallResponse(request.RoboId, 1).ToBytes(), ct);
            return;
        }

        await roboRepository.UpdateStateAsync(characterId, request.RoboId, (uint)RoboState.InMyRoom, ct);

        var map = new CharacterMapData
        {
            ChannelId = checked((uint)session.ChannelId),
            MapId = session.MapId,
            Movement = new MovementData(session.X, session.Y, session.Z, session.Rotation, MovementType.Stopped),
        };
        var stateUpdate = new NotifyUpdateRoboState(request.RoboId, robo.Character.SlotId, (uint)RoboState.InMyRoom, map);
        await session.SendAsync(PacketType.NotifyUpdateRoboState, stateUpdate.ToBytes(), ct);
        await session.SendAsync(ResponseType, new RoboCallResponse(request.RoboId, 0).ToBytes(), ct);
    }
}
