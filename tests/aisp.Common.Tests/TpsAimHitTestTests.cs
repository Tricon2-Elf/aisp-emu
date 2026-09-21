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
        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, Mob));
    }

    [Fact]
    public void HitsWhenAimedAtTorso()
    {
        var torso = Mob with { Y = Mob.Y + 80f };
        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, torso));
    }

    [Fact]
    public void HitsWhenAimedAtGroundByTheMob()
    {
        var groundAtFeet = Mob with { Y = 0f };
        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, groundAtFeet));
    }

    [Fact]
    public void HitsWhenAimedPastTheMob()
    {
        var past = Mob with { Z = Mob.Z + 400f };
        Assert.True(TpsAimHitTest.HitsPrototypeMob(Player, past));
    }

    [Fact]
    public void MissesWhenTargetStopsShortOfTheCylinder()
    {
        var wallInFront = Player with { Z = Player.Z + 50f };
        Assert.False(TpsAimHitTest.HitsPrototypeMob(Player, wallInFront));
    }

    [Fact]
    public void MissesWhenAimedWellAboveTheMob()
    {
        var high = Mob with { Y = Mob.Y + 500f };
        Assert.False(TpsAimHitTest.HitsPrototypeMob(Player, high));
    }

    [Fact]
    public void MissesWhenAimedBesideTheMob()
    {
        var beside = Mob with { X = Mob.X + 400f };
        Assert.False(TpsAimHitTest.HitsPrototypeMob(Player, beside));
    }
}
