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
        TpsCombatTestState.ResetPlayer(session.CharacterId);
        const uint wipeMobId = 2000099;
        TpsCombatTestState.ResetMonster(wipeMobId, ownerCharacterId: session.CharacterId);
        Assert.True(TpsCombatTestState.GetHp(wipeMobId) > 0);

        await TpsMissionGameOver.SendAsync(
            session,
            NullLogger.Instance,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, TpsCombatTestState.GetHp(wipeMobId));
        Assert.Contains(
            session.Sent,
            packet =>
                packet.Type == PacketType.NotifyDisappearChara
                && new PacketReader(packet.Payload).ReadUInt() == wipeMobId
        );
        Assert.Contains(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionSituationMessage
        );
        Assert.Contains(session.Sent, packet => packet.Type == PacketType.NotifyMissionResultOpen);
        Assert.Contains(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionOutmapChoiceOpen
        );
        Assert.Contains(
            session.Sent,
            packet =>
                packet.Type == PacketType.NotifyMissionAction
                && new PacketReader(packet.Payload).ReadUInt()
                    == TpsMissionGameOver.MissionActionEnd
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
        TpsCombatTestState.ResetPlayer(session.CharacterId);
        const uint clearMobId = 2000299;
        TpsCombatTestState.ResetMonster(clearMobId, ownerCharacterId: session.CharacterId);
        TpsCombatTestState.RecordShot(session.CharacterId, hit: true);
        TpsCombatTestState.DealDamage(clearMobId, TpsPrototypeConstants.DefaultHitPoints);
        TpsCombatTestState.RecordKill(session.CharacterId);
        Assert.True(TpsCombatTestState.AllOwnedMobsDefeated(session.CharacterId));

        await TpsMissionGameOver.SendSuccessAsync(
            session,
            NullLogger.Instance,
            TestContext.Current.CancellationToken
        );

        Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.NotifyEmotionChara);
        Assert.Contains(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionSituationMessage
        );
        Assert.Contains(
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
