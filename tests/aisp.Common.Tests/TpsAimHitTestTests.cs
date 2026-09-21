using System.Numerics;
using aisp.Common.Game;

namespace aisp.Common.Tests;

public sealed class TpsAimHitTestTests
{
    private static readonly Vector3 Player = new(
        TpsPrototypeConstants.PlayerSpawnX,
        TpsPrototypeConstants.PlayerSpawnY,
        TpsPrototypeConstants.PlayerSpawnZ
    );

    private static readonly Vector3 Mob = new(
        TpsPrototypeConstants.MobSpawnX,
        TpsPrototypeConstants.MobSpawnY,
        TpsPrototypeConstants.MobSpawnZ
    );

    [Fact]
    public void HitsWhenAimedAtMobCenter()
    {
        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, Mob, Mob));
    }

    [Fact]
    public void HitsWhenAimedAtTorso()
    {
        var torso = Mob with { Y = Mob.Y + 80f };
        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, torso, Mob));
    }

    [Fact]
    public void HitsWhenAimedAtGroundByTheMob()
    {
        var groundAtFeet = Mob with { Y = 0f };
        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, groundAtFeet, Mob));
    }

    [Fact]
    public void HitsWhenAimedPastTheMob()
    {
        var past = Mob with { Z = Mob.Z + 400f };
        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, past, Mob));
    }

    [Fact]
    public void MissesWhenTargetStopsShortOfTheCylinder()
    {
        var wallInFront = Player with { Z = Player.Z + 50f };
        Assert.False(TpsAimHitTest.HitsPrototypeMob(Player, wallInFront, Mob));
    }

    [Fact]
    public void MissesWhenAimedWellAboveTheMob()
    {
        var high = Mob with { Y = Mob.Y + 500f };
        Assert.False(TpsAimHitTest.HitsPrototypeMob(Player, high, Mob));
    }

    [Fact]
    public void MissesWhenAimedBesideTheMob()
    {
        var beside = Mob with { X = Mob.X + 400f };
        Assert.False(TpsAimHitTest.HitsPrototypeMob(Player, beside, Mob));
    }

    [Fact]
    public void HitsWhenMobHasWanderedToTheAimPoint()
    {
        TpsCombatTestState.ResetMonster(TpsPrototypeConstants.MobObjectId);
        var wandered = Mob with { X = Mob.X + 250f, Z = Mob.Z + 80f };
        TpsCombatTestState.SetMobPosition(TpsPrototypeConstants.MobObjectId, wandered);

        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, Mob));
        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, wandered));
        var beside = Mob with { X = Mob.X + 400f };
        Assert.False(TpsAimHitTest.HitsPrototypeMob(Player, beside));
        TpsCombatTestState.ResetMonster(TpsPrototypeConstants.MobObjectId);
    }

    [Fact]
    public void HitsEitherEndOfAnActiveWanderStep()
    {
        TpsCombatTestState.ResetMonster(TpsPrototypeConstants.MobObjectId);
        var dest = Mob with { X = Mob.X + 250f, Z = Mob.Z + 80f };
        TpsCombatTestState.SetMobMove(TpsPrototypeConstants.MobObjectId, Mob, dest, 10_000);

        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, Mob));
        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, dest));
        TpsCombatTestState.ResetMonster(TpsPrototypeConstants.MobObjectId);
    }
}
