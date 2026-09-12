using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class AreaBattleAttackHandlerTests
{
    [Fact]
    public async Task AttackStart_AcksAndSendsBattleReportToShooter()
    {
        TpsCombatTestState.ResetMonster(TpsPrototypeConstants.MobObjectId);
        var state = new SharedState();
        var session = new CapturingPlayerSession
        {
            CharacterId = 42_4242,
            LockedTargetId = TpsPrototypeConstants.MobObjectId,
        };
        state.RegisterClient(ServerType.Area, session);

        var handler = new AreaBattleAttackStartHandler(
            NullLogger<AreaBattleAttackStartHandler>.Instance,
            state
        );
        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(PacketType.BattleAttackStartResponse, session.Sent[0].Type);
        var report = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.NotifyBattleReportTargetObj
        );
        var reader = new PacketReader(report.Payload);
        Assert.Equal(session.CharacterId, reader.ReadUInt());
        Assert.Equal(TpsPrototypeConstants.BattleReportAttackAction, reader.ReadUInt());
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyUpdateHitpoint
        );
    }

    [Fact]
    public void BattleAttackExecRequest_MatchesClientSendOpcode()
    {
        Assert.Equal(0xC38D, (ushort)PacketType.BattleAttackExecRequest);
        Assert.Equal(50061, (ushort)PacketType.BattleAttackExecRequest);
    }

    [Fact]
    public async Task AttackExec_AppliesShotAndAcks()
    {
        TpsCombatTestState.ResetMonster(TpsPrototypeConstants.MobObjectId);
        var state = new SharedState();
        var session = new CapturingPlayerSession
        {
            CharacterId = 42_4243,
            LockedTargetId = TpsPrototypeConstants.MobObjectId,
        };
        state.RegisterClient(ServerType.Area, session);

        var handler = new AreaBattleAttackExecHandler(
            NullLogger<AreaBattleAttackExecHandler>.Instance,
            state
        );
        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(PacketType.BattleAttackExecResponse, session.Sent[0].Type);
        Assert.Contains(session.Sent, packet => packet.Type == PacketType.NotifyUpdateHitpoint);
        Assert.Contains(session.Sent, packet => packet.Type == PacketType.NotifyUpdateTank);

        var recover = session.Sent.Last(packet =>
            packet.Type == PacketType.NotifyBattleReportTargetObj
        );
        var reader = new PacketReader(recover.Payload);
        Assert.Equal(session.CharacterId, reader.ReadUInt());
        Assert.Equal(TpsPrototypeConstants.BattleReportRecoverAction, reader.ReadUInt());
        Assert.Equal((byte)0, reader.ReadByte());
        Assert.Equal(TpsPrototypeConstants.DefaultSkills[0], reader.ReadUInt());
    }

    [Fact]
    public async Task AttackCancel_AcksZero()
    {
        var session = new CapturingPlayerSession { CharacterId = 42 };
        var handler = new AreaBattleAttackCancelHandler(
            NullLogger<AreaBattleAttackCancelHandler>.Instance
        );
        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Single(session.Sent);
        Assert.Equal(PacketType.BattleAttackCancelResponse, session.Sent[0].Type);
        Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());
    }

    [Fact]
    public async Task DashExec_AcksAndSendsDashBeginReport()
    {
        var state = new SharedState();
        var session = new CapturingPlayerSession { CharacterId = 42_4245 };
        state.RegisterClient(ServerType.Area, session);

        var handler = new AreaBattleDashExecHandler(
            NullLogger<AreaBattleDashExecHandler>.Instance,
            state
        );
        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(PacketType.BattleDashExecResponse, session.Sent[0].Type);
        var report = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.NotifyBattleReportTargetObj
        );
        var reader = new PacketReader(report.Payload);
        Assert.Equal(session.CharacterId, reader.ReadUInt());
        Assert.Equal(TpsPrototypeConstants.BattleReportDashAction, reader.ReadUInt());
    }

    [Fact]
    public async Task DashFinish_AcksAndSendsDashEndReport()
    {
        var state = new SharedState();
        var session = new CapturingPlayerSession
        {
            CharacterId = 42_4244,
            LockedTargetId = TpsPrototypeConstants.MobObjectId,
        };
        state.RegisterClient(ServerType.Area, session);

        var handler = new AreaBattleDashFinishHandler(
            NullLogger<AreaBattleDashFinishHandler>.Instance,
            state
        );
        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(PacketType.BattleDashFinishResponse, session.Sent[0].Type);
        Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());

        var end = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.NotifyBattleReportTargetObj
        );
        var reader = new PacketReader(end.Payload);
        Assert.Equal(session.CharacterId, reader.ReadUInt());
        Assert.Equal(TpsPrototypeConstants.BattleReportDashEndAction, reader.ReadUInt());
        Assert.Equal((byte)0, reader.ReadByte());
        Assert.Equal(TpsPrototypeConstants.BattleReportDashEndSkillId, reader.ReadUInt());
    }
}
