using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaBattleTargetLockHandler(
    ILogger<AreaBattleTargetLockHandler> logger,
    SharedState state
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
        session.LockedTargetId = req.TargetObjectId;

        logger.LogInformation(
            "TPS Combat: Character {Id} locked on Monster {TargetId}",
            session.CharacterId,
            req.TargetObjectId
        );

        // recv_battle_target_lock_r treats a non-zero value as the object id whose
        // pending lock must be discarded. Zero acknowledges a successful lock.
        await session.SendAsync(ResponseType, new BattleTargetLockResponse(0).ToBytes(), ct);

        var lockNotify = new BattleTargetLockNotify(
            session.CharacterId,
            req.TargetObjectId,
            1
        ).ToBytes();
        foreach (var client in state.GetAreaPeers(session, includeSelf: false))
            await client.SendAsync(PacketType.NotifyBattleTargetLock, lockNotify, ct);
    }
}

public class AreaBattleTargetUnlockHandler(ILogger<AreaBattleTargetUnlockHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
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
        if (session.LockedTargetId != 0)
        {
            logger.LogDebug(
                "TPS Combat: Character {Id} unlock request (was target {TargetId}); no notify echo",
                session.CharacterId,
                session.LockedTargetId
            );
            session.LockedTargetId = 0;
        }

        return Task.CompletedTask;
    }
}

public class AreaBattleAttackStartHandler(ILogger<AreaBattleAttackStartHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
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
        logger.LogDebug("TPS Combat: Attack start from Character {Id}", session.CharacterId);
        await session.SendAsync(ResponseType, new BattleAttackStartResponse(0).ToBytes(), ct);
    }
}

public class AreaBattleAttackExecHandler(
    ILogger<AreaBattleAttackExecHandler> logger,
    SharedState state
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
        logger.LogInformation("TPS Combat: Character {Id} fired water gun!", session.CharacterId);

        await session.SendAsync(ResponseType, new BattleAttackExecResponse(0).ToBytes(), ct);

        var remainingTank = TpsCombatTestState.ConsumeTank(session.CharacterId);
        await session.SendAsync(
            PacketType.NotifyUpdateTank,
            new NotifyUpdateTank(session.CharacterId, remainingTank).ToBytes(),
            ct
        );

        uint targetId =
            session.LockedTargetId != 0
                ? session.LockedTargetId
                : TpsPrototypeConstants.MobObjectId;
        var (remHp, died, kills) = TpsCombatTestState.DealDamage(
            targetId,
            TpsPrototypeConstants.AttackDamage
        );

        logger.LogInformation(
            "Shot hit Monster {TargetId}! HP: {Hp}/100, Tank: {Tank}%",
            targetId,
            remHp,
            remainingTank
        );

        await session.SendAsync(
            PacketType.NotifyUpdateHitpoint,
            new NotifyUpdateHitpoint(targetId, (uint)remHp).ToBytes(),
            ct
        );

        if (!died)
            return;

        logger.LogInformation("Monster {TargetId} DEFEATED! Total kills: {Kills}", targetId, kills);

        await session.SendAsync(
            PacketType.NotifyEmotionChara,
            new NotifyEmotionChara(targetId, 3).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.NotifyMissionAction,
            new NotifyMissionAction(2, kills).ToBytes(),
            ct
        );

        session.LockedTargetId = 0;
        var unlockNotify = new NotifyBattleTargetUnlock(session.CharacterId).ToBytes();
        // Sending this back to the acting client invokes its unlock-send path again.
        foreach (var client in state.GetAreaPeers(session, includeSelf: false))
            await client.SendAsync(PacketType.NotifyBattleTargetUnlock, unlockNotify, ct);

        _ = Task.Run(
            async () =>
            {
                await Task.Delay(1000, ct);
                await session.SendAsync(
                    PacketType.NotifyDisappearChara,
                    new NotifyDisappearChara(targetId).ToBytes(),
                    ct
                );
            },
            ct
        );
    }
}

public class AreaBattleDashExecHandler(ILogger<AreaBattleDashExecHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
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
        logger.LogDebug("TPS Combat: Character {Id} executed dash", session.CharacterId);
        await session.SendAsync(ResponseType, new BattleDashExecResponse(0).ToBytes(), ct);
    }
}

public class AreaBattleDashFinishHandler(ILogger<AreaBattleDashFinishHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
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
        logger.LogDebug("TPS Combat: Character {Id} finished dash", session.CharacterId);
        await session.SendAsync(ResponseType, new BattleDashFinishResponse(0).ToBytes(), ct);
    }
}
