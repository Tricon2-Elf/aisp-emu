using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboGetListHandler(IRoboRepository roboRepository)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboGetListRequest;

    public PacketType ResponseType => PacketType.RoboGetListResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var characterId = checked((int)session.CharacterId);
        var ownedRobos = (await roboRepository.GetAllAsync(characterId, ct))
            .Take(RoboGetListResponse.MaximumRoboCount)
            .Select(robo => SharedState.PrepareOwnedRobo(robo, session))
            .ToList();
        await session.SendAsync(ResponseType, new RoboGetListResponse(ownedRobos).ToBytes(), ct);
    }
}
