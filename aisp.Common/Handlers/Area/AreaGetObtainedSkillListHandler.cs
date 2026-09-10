using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaGetObtainedSkillListHandler(
    IRoboRepository roboRepository,
    ILogger<AreaGetObtainedSkillListHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboGetObtainedSkillListRequest;
    public PacketType ResponseType => PacketType.RoboGetObtainedSkillListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GetObtainedSkillListRequest.FromBytes(payload.Span);
        var characterId = checked((int)session.CharacterId);

        var robo = await roboRepository.GetAsync(characterId, request.RoboId, ct);
        var roboLevel = robo?.Character.Progress?.Level ?? 1u;

        logger.LogInformation(
            "Giving TPS skills to RoboId {RoboId} (Level {Level}).",
            request.RoboId,
            roboLevel
        );

        List<uint> skillIds = [200090, 200000];

        if (roboLevel >= 2)
            skillIds.Add(200020);

        if (roboLevel >= 3)
            skillIds.Add(200010);

        if (roboLevel >= 5)
            skillIds.Add(200030);

        var response = new GetObtainedSkillListResponse(0, request.RoboId, skillIds);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
