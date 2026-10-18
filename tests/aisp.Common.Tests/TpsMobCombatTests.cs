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
    public async Task MobShot_RemovesOneHeart()
    {
        var session = new aisp.Common.Tests.Support.CapturingPlayerSession
        {
            CharacterId = 77,
            X = TpsPrototypeConstants.PlayerSpawnX,
            Y = TpsPrototypeConstants.PlayerSpawnY,
            Z = TpsPrototypeConstants.PlayerSpawnZ,
        };
        var mission = new MissionRegistry().Create(session);
        var mob = mission.AddPrototypeMob();
        mob.HasSentPath = true;
        await mission.TickAsync(
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            TestContext.Current.CancellationToken
        );
        mob.PhaseUntil = 0;
        await mission.TickAsync(
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            TestContext.Current.CancellationToken
        );

        var hp = mission.FindMember(77)!.HitPoints;
        Assert.False(mission.Finished);
        Assert.Equal(
            TpsPrototypeConstants.DefaultHitPoints - TpsPrototypeConstants.MobAttackDamage,
            hp
        );
        Assert.Equal(4, TpsPrototypeConstants.HeartsFromHp(hp));
    }
}
