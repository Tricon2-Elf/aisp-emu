using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaRoboSquireHandler(
    IRoboRepository roboRepository,
    ILogger<AreaRoboSquireHandler> logger,
    SharedState? state = null
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboSquireRequest;
    public PacketType ResponseType => PacketType.RoboSquireResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var characterId = checked((int)session.CharacterId);
        var request = RoboSquireRequest.FromBytes(payload.Span);
        var robo = await roboRepository.GetAsync(characterId, request.RoboId, ct);
        if (robo is null)
        {
            logger.LogWarning(
                "Character {CharacterId} tried to take unowned Robo {RoboId} outside",
                session.CharacterId,
                request.RoboId
            );
            await session.SendAsync(
                ResponseType,
                new RoboSquireResponse(request.RoboId, 1).ToBytes(),
                ct
            );
            return;
        }

        session.AccompanyingRoboIds.Add(request.RoboId);

        var map = new CharacterMapData
        {
            ChannelId = checked((uint)session.ChannelId),
            MapId = session.MapId,
            Movement = new MovementData(
                session.X,
                session.Y,
                session.Z,
                session.Rotation,
                MovementType.Stopped
            ),
        };

        // The response callback immediately activates the companion from cached RoboData.
        // Update that cache first so it observes state 2 (accompanying).
        var stateUpdate = new NotifyUpdateRoboState(
            request.RoboId,
            robo.Character.SlotId,
            (uint)RoboState.Accompanying,
            map
        );
        await session.SendAsync(PacketType.NotifyUpdateRoboState, stateUpdate.ToBytes(), ct);
        await session.SendAsync(
            ResponseType,
            new RoboSquireResponse(request.RoboId, 0).ToBytes(),
            ct
        );

        if (state is not null)
        {
            foreach (var peer in state.GetAreaPeers(session))
            {
                var remoteRobo = SharedState.PrepareRemoteRobo(robo, session);
                if (peer.VisibleRemoteRoboObjectIds.Add(remoteRobo.Character.SlotId))
                    await peer.SendAsync(
                        PacketType.NotifyRoboData,
                        new NotifyRoboData(0, remoteRobo).ToBytes(),
                        ct
                    );
            }
        }

        logger.LogInformation(
            "Robo {RoboId} now accompanies character {CharacterId}",
            request.RoboId,
            session.CharacterId
        );
    }
}
