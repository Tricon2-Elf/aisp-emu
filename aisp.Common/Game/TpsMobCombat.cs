using System.Numerics;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

/// <summary>
/// Prototype mob return-fire: range check, face the player, then the same
/// action 4/5/8 battle-report sequence the player gun uses.
/// </summary>
public static class TpsMobCombat
{
    public static bool IsPlayerInAttackRange(Vector3 mob, Vector3 player)
    {
        var dx = player.X - mob.X;
        var dz = player.Z - mob.Z;
        var range = TpsPrototypeConstants.MobAttackRange;
        return dx * dx + dz * dz <= range * range;
    }

    public static int YawToward(float dx, float dz)
    {
        if (dx * dx + dz * dz < 1e-6f)
            return TpsPrototypeConstants.MobSpawnRotation;

        var deg = (int)MathF.Round(MathF.Atan2(dx, dz) * (180f / MathF.PI));
        return ((deg % 360) + 360) % 360;
    }

    public static async Task FireAtPlayerAsync(
        IPlayerSession session,
        ILogger logger,
        CancellationToken ct
    )
    {
        var mobObjId = TpsPrototypeConstants.MobObjectId;
        var playerId = session.CharacterId;
        var mob = TpsCombatTestState.GetMobPosition(mobObjId);
        var player = new Vector3(session.X, session.Y, session.Z);
        var yaw = YawToward(player.X - mob.X, player.Z - mob.Z);
        var skillId = TpsPrototypeConstants.DefaultSkills[0];

        TpsCombatTestState.SetMobPosition(mobObjId, mob);
        await session.SendAsync(
            PacketType.AvatarNotifyMove,
            new AvatarNotifyMove(
                mobObjId,
                [new MovementData(mob.X, mob.Y, mob.Z, yaw, MovementType.Stopped)]
            ).ToBytes(),
            ct
        );

        await SendReportAsync(
            session,
            TpsPrototypeConstants.BattleReportAttackAction,
            skillId,
            player,
            ct
        );
        await Task.Delay(TpsPrototypeConstants.MobAttackWindupMs, ct);

        if (!session.IsTpsMode || TpsCombatTestState.GetHp(mobObjId) <= 0)
            return;

        await SendReportAsync(
            session,
            TpsPrototypeConstants.BattleReportShotAction,
            skillId,
            player,
            ct
        );

        var (remHp, died) = TpsCombatTestState.DealPlayerDamage(
            playerId,
            TpsPrototypeConstants.MobAttackDamage
        );
        var remHearts = TpsPrototypeConstants.HeartsFromHp(remHp);
        logger.LogInformation(
            "Mob {MobObjId} shot player {PlayerId}. HP: {Hp}/100, Hearts: {Hearts}/5",
            mobObjId,
            playerId,
            remHp,
            remHearts
        );

        await session.SendAsync(
            PacketType.NotifyUpdateHitpoint,
            new NotifyUpdateHitpoint(playerId, (uint)remHp).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.NotifyUpdateHeart,
            new NotifyUpdateHeart(playerId, (uint)remHearts).ToBytes(),
            ct
        );

        if (died)
        {
            await TpsMissionGameOver.SendAsync(session, logger, ct);
            return;
        }

        await Task.Delay(TpsPrototypeConstants.ShotRecoverDelayMs, ct);
        if (!session.IsTpsMode)
            return;

        await SendReportAsync(
            session,
            TpsPrototypeConstants.BattleReportRecoverAction,
            skillId,
            player,
            ct
        );

        await Task.Delay(TpsPrototypeConstants.MobAttackIntervalMs, ct);
    }

    private static Task SendReportAsync(
        IPlayerSession session,
        uint actionType,
        uint skillId,
        Vector3 targetPos,
        CancellationToken ct
    )
    {
        var usePos =
            actionType == TpsPrototypeConstants.BattleReportAttackAction
            || actionType == TpsPrototypeConstants.BattleReportShotAction;
        if (usePos)
        {
            return session.SendAsync(
                PacketType.NotifyBattleReportTargetPos,
                new NotifyBattleReportTargetPos(
                    TpsPrototypeConstants.MobObjectId,
                    actionType,
                    0,
                    skillId,
                    targetPos
                ).ToBytes(),
                ct
            );
        }

        return session.SendAsync(
            PacketType.NotifyBattleReportTargetObj,
            new NotifyBattleReportTargetObj(
                TpsPrototypeConstants.MobObjectId,
                actionType,
                0,
                skillId,
                session.CharacterId
            ).ToBytes(),
            ct
        );
    }
}
