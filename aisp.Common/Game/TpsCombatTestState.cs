using System.Collections.Concurrent;
using System.Numerics;

namespace aisp.Common.Game;

public readonly record struct TpsPendingAim(Vector3 TargetPos, Vector3 NowPos);

public readonly record struct TpsMobMove(Vector3 From, Vector3 To, long StartTick, int DurationMs);

public static class TpsCombatTestState
{
    private static readonly ConcurrentDictionary<uint, int> MonsterHp = new();
    private static readonly ConcurrentDictionary<uint, TpsMobMove> MonsterMove = new();
    private static readonly ConcurrentDictionary<uint, uint> PlayerTank = new();
    private static readonly ConcurrentDictionary<uint, TpsPendingAim> PendingAim = new();
    private static uint _killCount;

    public static int GetHp(uint mobObjId) =>
        MonsterHp.GetOrAdd(mobObjId, TpsPrototypeConstants.DefaultHitPoints);

    public static uint GetTank(uint characterId) =>
        PlayerTank.GetOrAdd(characterId, TpsPrototypeConstants.DefaultTank);

    public static uint ConsumeTank(
        uint characterId,
        uint amount = TpsPrototypeConstants.TankConsumePerShot
    )
    {
        var current = GetTank(characterId);
        var newTank = current >= amount ? current - amount : 0;
        PlayerTank[characterId] = newTank;
        return newTank;
    }

    public static (int RemainingHp, bool Died, uint TotalKills) DealDamage(
        uint mobObjId,
        int damage
    )
    {
        var current = GetHp(mobObjId);
        if (current <= 0)
            return (0, false, _killCount);

        var newHp = Math.Max(0, current - damage);
        MonsterHp[mobObjId] = newHp;

        var died = newHp == 0;
        if (died)
            _killCount++;

        return (newHp, died, _killCount);
    }

    public static void ResetMonster(
        uint mobObjId,
        int hitPoints = TpsPrototypeConstants.DefaultHitPoints
    )
    {
        MonsterHp[mobObjId] = hitPoints;
        SetMobPosition(
            mobObjId,
            new Vector3(
                TpsPrototypeConstants.MobSpawnX,
                TpsPrototypeConstants.MobSpawnY,
                TpsPrototypeConstants.MobSpawnZ
            )
        );
    }

    public static Vector3 GetMobPosition(uint mobObjId = TpsPrototypeConstants.MobObjectId)
    {
        if (!MonsterMove.TryGetValue(mobObjId, out var move))
        {
            return new Vector3(
                TpsPrototypeConstants.MobSpawnX,
                TpsPrototypeConstants.MobSpawnY,
                TpsPrototypeConstants.MobSpawnZ
            );
        }

        if (move.DurationMs <= 0)
            return move.To;

        var t = (Environment.TickCount64 - move.StartTick) / (float)move.DurationMs;
        if (t <= 0f)
            return move.From;
        if (t >= 1f)
            return move.To;

        return Vector3.Lerp(move.From, move.To, t);
    }

    public static bool TryGetMobPath(uint mobObjId, out Vector3 from, out Vector3 to)
    {
        if (MonsterMove.TryGetValue(mobObjId, out var move))
        {
            from = move.From;
            to = move.To;
            return true;
        }

        from = to = new Vector3(
            TpsPrototypeConstants.MobSpawnX,
            TpsPrototypeConstants.MobSpawnY,
            TpsPrototypeConstants.MobSpawnZ
        );
        return false;
    }

    public static void SetMobPosition(uint mobObjId, Vector3 position) =>
        MonsterMove[mobObjId] = new TpsMobMove(position, position, Environment.TickCount64, 0);

    public static void SetMobMove(uint mobObjId, Vector3 from, Vector3 to, int durationMs) =>
        MonsterMove[mobObjId] = new TpsMobMove(
            from,
            to,
            Environment.TickCount64,
            Math.Max(0, durationMs)
        );

    public static void SetPendingAim(uint characterId, Vector3 targetPos, Vector3 nowPos) =>
        PendingAim[characterId] = new TpsPendingAim(targetPos, nowPos);

    public static TpsPendingAim GetPendingAim(uint characterId) =>
        PendingAim.TryGetValue(characterId, out var aim) ? aim : default;

    /// <summary>
    /// Damage is applied on AttackExec. Collapse repeats within one client frame
    /// so a duplicated Exec does not double-hit.
    /// </summary>
    public static bool TryAcceptShot(uint characterId)
    {
        var now = Environment.TickCount64;
        if (LastShotAt.TryGetValue(characterId, out var last) && now - last < 120)
            return false;

        LastShotAt[characterId] = now;
        return true;
    }

    private static readonly ConcurrentDictionary<uint, long> LastShotAt = new();
}
