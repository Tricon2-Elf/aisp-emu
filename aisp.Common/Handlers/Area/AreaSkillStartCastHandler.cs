using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaSkillStartCastHandler(ILogger<AreaSkillStartCastHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.SkillStartCastRequest;
    public PacketType ResponseType => PacketType.SkillStartCastResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = SkillStartCastRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "Combat: Character {CharacterId} starting cast for Skill {SkillId} at Target {TargetId}",
            session.CharacterId,
            req.SkillId,
            req.TargetObjId
        );

        await session.SendAsync(ResponseType, new SkillStartCastResponse(0, 0.0f).ToBytes(), ct);
    }
}

public class AreaSkillExecHandler(ILogger<AreaSkillExecHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.SkillExecRequest;
    public PacketType ResponseType => PacketType.SkillExecResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = SkillExecRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "Combat: Character {CharacterId} hit Target {TargetId} with Skill {SkillId}",
            session.CharacterId,
            req.TargetObjId,
            req.SkillId
        );

        await session.SendAsync(ResponseType, new SkillExecResponse(0).ToBytes(), ct);

        if (req.TargetObjId != TpsPrototypeConstants.MobObjectId)
            return;

        logger.LogInformation("Monster defeated via skill!");
        await session.SendAsync(
            PacketType.NotifyEmotionChara,
            new NotifyEmotionChara(req.TargetObjId, 3).ToBytes(),
            ct
        );

        await Task.Delay(1000, ct);
        await session.SendAsync(
            PacketType.NotifyDisappearChara,
            new NotifyDisappearChara(req.TargetObjId).ToBytes(),
            ct
        );

        await session.SendAsync(
            PacketType.NotifyMissionAction,
            new NotifyMissionAction(2, 0).ToBytes(),
            ct
        );
        // Unnamed pad packet from the prototype skill-kill path.
        await session.SendAsync((PacketType)0x6E62, new byte[12], ct);
    }
}
