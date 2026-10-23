using aisp.Common.Game;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class TpsMissionGameOverTests
{
    [Fact]
    public async Task SendAsync_OpensFailUi()
    {
        var session = new CapturingPlayerSession { CharacterId = 99, IsTpsMode = true };
        var mission = new MissionRegistry().Create(session);
        const uint wipeMobId = 2000099;
        var mob = mission.AddMob(
            wipeMobId,
            TpsPrototypeConstants.DefaultHitPoints,
            new System.Numerics.Vector3(
                TpsPrototypeConstants.MobSpawnX,
                TpsPrototypeConstants.MobSpawnY,
                TpsPrototypeConstants.MobSpawnZ
            )
        );
        Assert.True(mob.HitPoints > 0);

        await TpsMissionGameOver.FailAsync(
            mission,
            NullLogger.Instance,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, mob.HitPoints);
        Assert.Contains(
            session.Sent,
            packet =>
                packet.Type == PacketType.NotifyDisappearChara
                && new PacketReader(packet.Payload).ReadUInt() == wipeMobId
        );
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionSituationMessage
        );
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionResultOpen
        );
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionOutmapChoiceOpen
        );
        var script = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.EventScriptPlayNotify
        );
        Assert.Equal(
            new EventScriptPlayNotify(TpsMissionGameOver.LoseScriptLabel()).ToBytes(),
            script.Payload
        );
        Assert.Contains(
            session.Sent,
            packet =>
                packet.Type == PacketType.NotifyMissionAction
                && new PacketReader(packet.Payload).ReadUInt()
                    == TpsMissionGameOver.MissionActionEnd
        );
        Assert.True(
            await TpsMissionGameOver.TryPresentClearAsync(
                session,
                TestContext.Current.CancellationToken
            )
        );
        var failResult = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionResultOpen
        );
        Assert.Equal(
            TpsMissionGameOver.FailResultCode,
            new PacketReader(failResult.Payload).ReadUInt()
        );
    }

    [Fact]
    public async Task SendSuccessAsync_OpensClearUi()
    {
        var session = new CapturingPlayerSession { CharacterId = 199, IsTpsMode = true };
        var mission = new MissionRegistry().Create(session);
        const uint clearMobId = 2000299;
        var mob = mission.AddMob(
            clearMobId,
            0,
            new System.Numerics.Vector3(
                TpsPrototypeConstants.MobSpawnX,
                TpsPrototypeConstants.MobSpawnY,
                TpsPrototypeConstants.MobSpawnZ
            )
        );
        var member = mission.FindMember(session.CharacterId)!;
        member.Shots = 1;
        member.Hits = 1;
        member.Kills = 1;
        Assert.Equal(0, mob.HitPoints);

        await TpsMissionGameOver.ClearAsync(
            mission,
            NullLogger.Instance,
            TestContext.Current.CancellationToken
        );

        Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.NotifyEmotionChara);
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionSituationMessage
        );
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionResultOpen
        );
        var script = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.EventScriptPlayNotify
        );
        Assert.Equal(
            new EventScriptPlayNotify(TpsMissionGameOver.WinScriptLabel()).ToBytes(),
            script.Payload
        );
        Assert.True(
            await TpsMissionGameOver.TryPresentClearAsync(
                session,
                TestContext.Current.CancellationToken
            )
        );
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionOutmapChoiceOpen
        );
        var result = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionResultOpen
        );
        var reader = new PacketReader(result.Payload);
        Assert.Equal(TpsMissionGameOver.SuccessResultCode, reader.ReadUInt());
        Assert.Equal((uint)MissionResultGrade.S, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        reader.ReadUInt();
        Assert.Equal(1u, reader.ReadUInt());
        reader.ReadUInt();
        Assert.Equal(100u, reader.ReadUInt());
    }
}
