using System.Numerics;
using aisp.Common.Game;

namespace aisp.Common.Tests;

public sealed class TpsMobCombatTests
{
    [Fact]
    public void InRangeWhenPlayerIsAtSpawnDistance()
    {
        var mob = new Vector3(
            TpsPrototypeConstants.MobSpawnX,
            TpsPrototypeConstants.MobSpawnY,
            TpsPrototypeConstants.MobSpawnZ
        );
        var player = new Vector3(
            TpsPrototypeConstants.PlayerSpawnX,
            TpsPrototypeConstants.PlayerSpawnY,
            TpsPrototypeConstants.PlayerSpawnZ
        );

        Assert.True(TpsMobCombat.IsPlayerInAttackRange(mob, player));
    }

    [Fact]
    public void OutOfRangeWhenPlayerIsFar()
    {
        var mob = new Vector3(
            TpsPrototypeConstants.MobSpawnX,
            TpsPrototypeConstants.MobSpawnY,
            TpsPrototypeConstants.MobSpawnZ
        );
        var player = mob with { X = mob.X + TpsPrototypeConstants.MobAttackRange + 50f };

        Assert.False(TpsMobCombat.IsPlayerInAttackRange(mob, player));
    }

    [Fact]
    public void DealPlayerDamage_RemovesOneHeart()
    {
        const uint playerId = 77;
        TpsCombatTestState.ResetPlayer(playerId);
        var (hp, died) = TpsCombatTestState.DealPlayerDamage(
            playerId,
            TpsPrototypeConstants.MobAttackDamage
        );

        Assert.False(died);
        Assert.Equal(
            TpsPrototypeConstants.DefaultHitPoints - TpsPrototypeConstants.MobAttackDamage,
            hp
        );
        Assert.Equal(4, TpsPrototypeConstants.HeartsFromHp(hp));
        TpsCombatTestState.ResetPlayer(playerId);
    }
}
