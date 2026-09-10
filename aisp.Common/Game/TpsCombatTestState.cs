using System.Collections.Concurrent;

namespace aisp.Common.Game;

public static class TpsCombatTestState
{
    private static readonly ConcurrentDictionary<uint, int> MonsterHp = new();
    private static readonly ConcurrentDictionary<uint, uint> PlayerTank = new();
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
    ) => MonsterHp[mobObjId] = hitPoints;

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
