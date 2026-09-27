using System.Collections.Concurrent;
using System.Numerics;

namespace aisp.Common.Game;

public readonly record struct TpsPendingAim(Vector3 TargetPos, Vector3 NowPos);

public readonly record struct TpsMobMove(Vector3 From, Vector3 To, long StartTick, int DurationMs);

public readonly record struct TpsMissionStats(
    uint Shots,
    uint Hits,
    uint Kills,
    uint Downs,
    uint ClearTimeSeconds
)
{
    public uint HitRatePercent => Shots == 0 ? 0 : Hits * 100 / Shots;
}

public static class TpsCombatTestState
{
    private static readonly ConcurrentDictionary<uint, int> MonsterHp = new();
    private static readonly ConcurrentDictionary<uint, int> PlayerHp = new();
    private static readonly ConcurrentDictionary<uint, TpsMobMove> MonsterMove = new();
    private static readonly ConcurrentDictionary<
        uint,
        ConcurrentDictionary<uint, byte>
    > PlayerMobs = new();
    private static readonly ConcurrentDictionary<uint, uint> PlayerTank = new();
    private static readonly ConcurrentDictionary<uint, TpsPendingAim> PendingAim = new();
    private static readonly ConcurrentDictionary<uint, byte> MissionFinished = new();
    private static readonly ConcurrentDictionary<uint, TpsShotStats> ShotStats = new();
    private static uint _killCount;

    private struct TpsShotStats
    {
        public uint Shots;
        public uint Hits;
        public uint Kills;
        public uint Downs;
        public long StartTick;
    }

    public static int GetHp(uint mobObjId) =>
        MonsterHp.GetOrAdd(mobObjId, TpsPrototypeConstants.DefaultHitPoints);

    public static int GetPlayerHp(uint characterId) =>
        PlayerHp.GetOrAdd(characterId, TpsPrototypeConstants.DefaultHitPoints);

    public static void ResetPlayer(
        uint characterId,
        int hitPoints = TpsPrototypeConstants.DefaultHitPoints
    )
    {
        PlayerHp[characterId] = hitPoints;
        PlayerTank[characterId] = TpsPrototypeConstants.DefaultTank;
        MissionFinished.TryRemove(characterId, out _);
        ShotStats[characterId] = new TpsShotStats { StartTick = Environment.TickCount64 };
    }

    public static void RecordShot(uint characterId, bool hit)
    {
        ShotStats.AddOrUpdate(
            characterId,
            _ => new TpsShotStats
            {
                Shots = 1,
                Hits = hit ? 1u : 0,
                StartTick = Environment.TickCount64,
            },
            (_, stats) =>
            {
                stats.Shots++;
                if (hit)
                    stats.Hits++;
                return stats;
            }
        );
    }

    public static void RecordKill(uint characterId)
    {
        ShotStats.AddOrUpdate(
            characterId,
            _ => new TpsShotStats { Kills = 1, StartTick = Environment.TickCount64 },
            (_, stats) =>
            {
                stats.Kills++;
                return stats;
            }
        );
    }

    public static TpsMissionStats GetMissionStats(uint characterId)
    {
        var stats = ShotStats.TryGetValue(characterId, out var raw) ? raw : default;
        var start = stats.StartTick == 0 ? Environment.TickCount64 : stats.StartTick;
        var elapsed = (uint)Math.Max(0, (Environment.TickCount64 - start) / 1000);
        return new TpsMissionStats(stats.Shots, stats.Hits, stats.Kills, stats.Downs, elapsed);
    }

    public static bool TryFinishMission(uint characterId) => MissionFinished.TryAdd(characterId, 1);

    public static bool AllOwnedMobsDefeated(uint characterId) =>
        PlayerMobs.TryGetValue(characterId, out var mobs) && mobs.IsEmpty;

    public static (int RemainingHp, bool Died) DealPlayerDamage(uint characterId, int damage)
    {
        var current = GetPlayerHp(characterId);
        if (current <= 0)
            return (0, false);

        var newHp = Math.Max(0, current - damage);
        PlayerHp[characterId] = newHp;
        var died = newHp == 0;
        if (died)
        {
            ShotStats.AddOrUpdate(
                characterId,
                _ => new TpsShotStats { Downs = 1, StartTick = Environment.TickCount64 },
                (_, stats) =>
                {
                    stats.Downs++;
                    return stats;
                }
            );
        }

        return (newHp, died);
    }

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
        {
            _killCount++;
            UnregisterMob(mobObjId);
        }

        return (newHp, died, _killCount);
    }

    public static void ResetMonster(
        uint mobObjId,
        int hitPoints = TpsPrototypeConstants.DefaultHitPoints,
        uint ownerCharacterId = 0
    )
    {
        MonsterHp[mobObjId] = hitPoints;
        UnregisterMob(mobObjId);
        if (ownerCharacterId != 0)
            PlayerMobs.GetOrAdd(ownerCharacterId, _ => new()).TryAdd(mobObjId, 1);
        SetMobPosition(
            mobObjId,
            new Vector3(
                TpsPrototypeConstants.MobSpawnX,
                TpsPrototypeConstants.MobSpawnY,
                TpsPrototypeConstants.MobSpawnZ
            )
        );
    }

    public static uint[] DeactivateMobsFor(uint characterId)
    {
        if (!PlayerMobs.TryRemove(characterId, out var mobs))
            return [];

        var ids = mobs.Keys.ToArray();
        foreach (var id in ids)
        {
            MonsterHp[id] = 0;
            MonsterMove.TryRemove(id, out _);
        }

        return ids;
    }

    private static void UnregisterMob(uint mobObjId)
    {
        foreach (var mobs in PlayerMobs.Values)
            mobs.TryRemove(mobObjId, out _);
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
