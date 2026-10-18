using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaBattleTargetLockHandler(
    ILogger<AreaBattleTargetLockHandler> logger,
    MissionRegistry missions
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.BattleTargetLockRequest;
    public PacketType ResponseType => PacketType.BattleTargetLockResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = BattleTargetLockRequest.FromBytes(payload.Span);
        var mission = missions.TryGet(session);
        var member = mission?.FindMember(session.CharacterId);
        if (member is not null)
            member.LockedTargetId = req.TargetObjectId;

        logger.LogInformation(
            "TPS Combat: Character {Id} locked on Monster {TargetId}",
            session.CharacterId,
            req.TargetObjectId
        );

        // recv_battle_target_lock_r treats a non-zero value as the object id whose
        // pending lock must be discarded. Zero acknowledges a successful lock.
        await session.SendAsync(ResponseType, new BattleTargetLockResponse(0).ToBytes(), ct);

        if (mission is null || member is null)
            return;

        var lockNotify = new BattleTargetLockNotify(
            session.CharacterId,
            req.TargetObjectId,
            1
        ).ToBytes();
        foreach (var other in mission.Members)
        {
            if (other.ConnectionId == session.ConnectionId)
                continue;
            await other.Session.SendAsync(PacketType.NotifyBattleTargetLock, lockNotify, ct);
        }
    }
}

public class AreaBattleTargetUnlockHandler(
    ILogger<AreaBattleTargetUnlockHandler> logger,
    MissionRegistry missions
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.BattleTargetUnlockRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        // Do NOT echo NotifyBattleTargetUnlock here. Client recv path is:
        //   notify → sub_4F7E10(id!=0) → sub_4F7710(null) → send unlock again
        // while the local sticky target ptr (this+36) is still set → 60 Hz ping-pong.
        // Official/emu2 ignore the request; clear server lock state only.
        var member = missions.TryGet(session)?.FindMember(session.CharacterId);
        if (member is { LockedTargetId: not 0 })
        {
            logger.LogDebug(
                "TPS Combat: Character {Id} unlock request (was target {TargetId}); no notify echo",
                session.CharacterId,
                member.LockedTargetId
            );
            member.LockedTargetId = 0;
        }

        return Task.CompletedTask;
    }
}

public class AreaBattleAttackStartHandler(
    ILogger<AreaBattleAttackStartHandler> logger,
    MissionRegistry missions
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.BattleAttackStartRequest;
    public PacketType ResponseType => PacketType.BattleAttackStartResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = BattleAttackStartRequest.FromBytes(payload.Span);
        var mission = missions.TryGet(session);
        mission?.SetAim(session.CharacterId, req.TargetPos, req.NowPos);

        logger.LogInformation("TPS Combat: Attack start from Character {Id}", session.CharacterId);
        await session.SendAsync(ResponseType, new BattleAttackStartResponse(0).ToBytes(), ct);

        // start_r(0) leaves the local TPS phase at 4. The fire motion and
        // send_battle_attack_exec callback are queued by CTPSActionReport
        // (recv_notify_battle_report_target_obj), not by the start reply.
        var member = mission?.FindMember(session.CharacterId);
        if (mission is null || member is null)
            return;

        await mission.BroadcastReportAsync(
            member,
            TpsPrototypeConstants.BattleReportAttackAction,
            ct
        );
    }
}

public class AreaBattleAttackCancelHandler(ILogger<AreaBattleAttackCancelHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.BattleAttackCancelRequest;
    public PacketType ResponseType => PacketType.BattleAttackCancelResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogDebug("TPS Combat: Attack cancel from Character {Id}", session.CharacterId);
        await session.SendAsync(ResponseType, new BattleAttackCancelResponse(0).ToBytes(), ct);
    }
}

public class AreaBattleAttackExecHandler(
    ILogger<AreaBattleAttackExecHandler> logger,
    MissionRegistry missions
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.BattleAttackExecRequest;
    public PacketType ResponseType => PacketType.BattleAttackExecResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = BattleAttackExecRequest.FromBytes(payload.Span);
        logger.LogInformation("TPS Combat: Character {Id} fired water gun!", session.CharacterId);

        await session.SendAsync(ResponseType, new BattleAttackExecResponse(0).ToBytes(), ct);

        var mission = missions.TryGet(session);
        var member = mission?.FindMember(session.CharacterId);
        if (mission is null || member is null)
            return;

        await mission.ApplyPlayerShotAsync(logger, member, req.NowPos, ct);
        // Action 5 starts CTPSTimeBlaze + SE. Do not send action 8 in the same
        // tick — that queues phase 0 and cuts the stream off immediately.
        // Recover is queued on the mission and sent by its loop.
        await mission.BroadcastReportAsync(
            member,
            TpsPrototypeConstants.BattleReportShotAction,
            ct
        );
    }
}

public class AreaBattleDashExecHandler(
    ILogger<AreaBattleDashExecHandler> logger,
    MissionRegistry missions
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.BattleDashExecRequest;
    public PacketType ResponseType => PacketType.BattleDashExecResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var mission = missions.TryGet(session);
        var member = mission?.FindMember(session.CharacterId);
        if (mission is null || member is null)
        {
            await session.SendAsync(ResponseType, new BattleDashExecResponse(1).ToBytes(), ct);
            return;
        }

        if (member.Stamina < TpsPrototypeConstants.StaminaSprintReady)
        {
            logger.LogDebug(
                "TPS Combat: Sprint rejected for Character {Id} (Stamina not full: {Stamina})",
                session.CharacterId,
                member.Stamina
            );
            await session.SendAsync(ResponseType, new BattleDashExecResponse(1).ToBytes(), ct);
            await mission.BroadcastReportAsync(
                member,
                TpsPrototypeConstants.BattleReportDashEndAction,
                TpsPrototypeConstants.BattleReportDashEndSkillId,
                ct
            );
            return;
        }

        logger.LogDebug(
            "TPS Combat: Character {Id} executed dash (sprint start)",
            session.CharacterId
        );
        member.IsSprinting = true;
        await session.SendAsync(ResponseType, new BattleDashExecResponse(0).ToBytes(), ct);

        // Right-click sets controller+224=1 (keep sliding forward) and phase 7.
        // Action 0x1C queues phase 8 + the dash motion so the local machine can
        // advance and later send finish. Stamina drain is the mission loop.
        await mission.BroadcastReportAsync(
            member,
            TpsPrototypeConstants.BattleReportDashAction,
            TpsPrototypeConstants.BattleReportDashSkillId,
            ct
        );
    }
}

public class AreaBattleDashFinishHandler(
    ILogger<AreaBattleDashFinishHandler> logger,
    MissionRegistry missions
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.BattleDashFinishRequest;
    public PacketType ResponseType => PacketType.BattleDashFinishResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogDebug(
            "TPS Combat: Character {Id} finished dash (sprint stop)",
            session.CharacterId
        );
        await session.SendAsync(ResponseType, new BattleDashFinishResponse(0).ToBytes(), ct);

        var mission = missions.TryGet(session);
        var member = mission?.FindMember(session.CharacterId);
        if (mission is null || member is null)
            return;

        member.IsSprinting = false;
        // Action 8 only sets phase 0. Right-click dash keeps sliding until
        // action 0x1D / skill 0 runs sub_4E06E0 (clears controller+224).
        // Regen starts on the next mission tick because the member is not sprinting.
        await mission.BroadcastReportAsync(
            member,
            TpsPrototypeConstants.BattleReportDashEndAction,
            TpsPrototypeConstants.BattleReportDashEndSkillId,
            ct
        );
    }
}

public class AreaBattleAttackBlazeHandler(
    ILogger<AreaBattleAttackBlazeHandler> logger,
    MissionRegistry missions
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.BattleAttackBlazeRequest;
    public PacketType ResponseType => PacketType.BattleAttackBlazeResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = BattleAttackBlazeRequest.FromBytes(payload.Span);
        missions.TryGet(session)?.SetAim(session.CharacterId, req.TargetPos, req.NowPos);
        logger.LogDebug("TPS Combat: Blaze tick from Character {Id}", session.CharacterId);
        await session.SendAsync(ResponseType, new BattleAttackBlazeResponse(0).ToBytes(), ct);
    }
}
