using aisp.Common.Game;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class MissionInstanceTests
{
    [Fact]
    public void TryAddMember_RejectsAFifthPlayer()
    {
        var registry = new MissionRegistry();
        var mission = registry.Create(new CapturingPlayerSession { CharacterId = 1 });

        Assert.True(registry.TryAddMember(mission, new CapturingPlayerSession { CharacterId = 2 }));
        Assert.True(registry.TryAddMember(mission, new CapturingPlayerSession { CharacterId = 3 }));
        Assert.True(registry.TryAddMember(mission, new CapturingPlayerSession { CharacterId = 4 }));
        Assert.False(
            registry.TryAddMember(mission, new CapturingPlayerSession { CharacterId = 5 })
        );
        Assert.Equal(MissionInstance.MaxMembers, mission.Members.Count);
    }

    [Fact]
    public async Task ShotDamage_IsSharedWithEveryMember()
    {
        var registry = new MissionRegistry();
        var shooter = new CapturingPlayerSession
        {
            CharacterId = 11,
            X = TpsPrototypeConstants.PlayerSpawnX,
            Y = TpsPrototypeConstants.PlayerSpawnY,
            Z = TpsPrototypeConstants.PlayerSpawnZ,
        };
        var partner = new CapturingPlayerSession { CharacterId = 12 };
        var mission = registry.Create(shooter);
        Assert.True(registry.TryAddMember(mission, partner));
        var mob = mission.AddPrototypeMob();
        var member = mission.FindMember(shooter.CharacterId)!;
        member.LockedTargetId = mob.ObjectId;
        var hpBefore = mob.HitPoints;

        var handler = new aisp.Common.Handlers.Area.AreaBattleAttackExecHandler(
            NullLogger<aisp.Common.Handlers.Area.AreaBattleAttackExecHandler>.Instance,
            registry
        );
        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            shooter,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(hpBefore - TpsPrototypeConstants.AttackDamage, mob.HitPoints);
        Assert.Contains(shooter.Sent, packet => packet.Type == PacketType.NotifyUpdateHitpoint);
        Assert.Contains(partner.Sent, packet => packet.Type == PacketType.NotifyUpdateHitpoint);
    }
}
