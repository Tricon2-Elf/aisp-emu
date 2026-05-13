using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMapDataEnterEndHandler(SharedState state, ILogger<AreaMapDataEnterEndHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MapDataEnterEndRequest;
    public PacketType ResponseType => PacketType.MapDataEnterEndResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        session.IsMapTransitionPending = false;
        await session.SendAsync(ResponseType, new MapDataEnterEndResponse().ToBytes(), ct);

        var myChar = session.Character ?? session.User!.Characters.FirstOrDefault();
        if (myChar == null)
            return;

        var myPos = new MovementData(session.X, session.Y, session.Z, session.Rotation, MovementType.Stopped);

        var spawnMeForPeersPacket = AreasvEnterHandler.CreateNotify(myChar, session.CharacterId, 1, myPos);
        if (session.NeedsPostLoadSelfAvatarNotify)
        {
            logger.LogInformation("Sending AvatarNotifyData to {ConnectionId} for character {CharacterId}", session.ConnectionId, myChar.Id);
            var spawnMeForSelfPacket = AreasvEnterHandler.CreateNotify(myChar, session.CharacterId, 0, myPos);
            await session.SendAsync(PacketType.AvatarNotifyData, spawnMeForSelfPacket, ct);
            session.NeedsPostLoadSelfAvatarNotify = false;
        }
        foreach (var other in state.GetAreaPeers(session))
        {
            await other.SendAsync(PacketType.AvatarNotifyData, spawnMeForPeersPacket, ct);
            logger.LogInformation("Sending AvatarNotifyData to {ConnectionId} for othercharacter {CharacterId}", other.ConnectionId, myChar.Id);
            var otherChar = other.Character ?? other.User?.Characters.FirstOrDefault();
            if (otherChar != null)
            {
                var otherPos = new MovementData(other.X, other.Y, other.Z, other.Rotation, MovementType.Stopped);
                var spawnOtherForMe = AreasvEnterHandler.CreateNotify(otherChar, other.CharacterId, 1, otherPos);
                await session.SendAsync(PacketType.AvatarNotifyData, spawnOtherForMe, ct);
            }
        }
    }
}
