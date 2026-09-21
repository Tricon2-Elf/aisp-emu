using System.Numerics;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class AreaBattleAttackHandlerTests
{
    private static readonly Vector3 PlayerSpawn = new(
        TpsPrototypeConstants.PlayerSpawnX,
        TpsPrototypeConstants.PlayerSpawnY,
        TpsPrototypeConstants.PlayerSpawnZ
    );

    private static readonly Vector3 MobSpawn = new(
        TpsPrototypeConstants.MobSpawnX,
        TpsPrototypeConstants.MobSpawnY,
        TpsPrototypeConstants.MobSpawnZ
    );

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

        var reports = session
            .Sent.Where(packet => packet.Type == PacketType.NotifyBattleReportTargetObj)
            .ToList();
        var shot = Assert.Single(reports);
        var reader = new PacketReader(shot.Payload);
        Assert.Equal(session.CharacterId, reader.ReadUInt());
        Assert.Equal(TpsPrototypeConstants.BattleReportShotAction, reader.ReadUInt());
        Assert.Equal((byte)0, reader.ReadByte());
        Assert.Equal(TpsPrototypeConstants.DefaultSkills[0], reader.ReadUInt());
    }

    [Fact]
    public async Task AttackBlaze_AcksZeroAndStoresAim()
    {
        var session = new CapturingPlayerSession { CharacterId = 42_4253 };
        var handler = new AreaBattleAttackBlazeHandler(
            NullLogger<AreaBattleAttackBlazeHandler>.Instance
        );
        await handler.HandleAsync(
            WriteVec3s(MobSpawn, PlayerSpawn),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Single(session.Sent);
        Assert.Equal(PacketType.BattleAttackBlazeResponse, session.Sent[0].Type);
        Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());
        Assert.Equal(MobSpawn, TpsCombatTestState.GetPendingAim(session.CharacterId).TargetPos);
        Assert.Equal(PlayerSpawn, TpsCombatTestState.GetPendingAim(session.CharacterId).NowPos);
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

    [Fact]
    public async Task AttackExec_FreeAimHit_DealsDamage()
    {
        TpsCombatTestState.ResetMonster(TpsPrototypeConstants.MobObjectId);
        var hpBefore = TpsCombatTestState.GetHp(TpsPrototypeConstants.MobObjectId);
        var (_, session, start, exec) = CreateBattleHandlers(42_4250, locked: false);

        await start.HandleAsync(
            WriteVec3s(MobSpawn, PlayerSpawn),
            session,
            TestContext.Current.CancellationToken
        );
        await exec.HandleAsync(
            WriteVec3(PlayerSpawn),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Contains(session.Sent, packet => packet.Type == PacketType.NotifyUpdateHitpoint);
        Assert.Contains(session.Sent, packet => packet.Type == PacketType.NotifyUpdateTank);
        var shot = Assert.Single(
            session.Sent.Where(packet => packet.Type == PacketType.NotifyBattleReportTargetPos),
            packet =>
            {
                var body = new PacketReader(packet.Payload);
                body.ReadUInt();
                return body.ReadUInt() == TpsPrototypeConstants.BattleReportShotAction;
            }
        );
        Assert.Equal(session.CharacterId, new PacketReader(shot.Payload).ReadUInt());
        Assert.Equal(
            hpBefore - TpsPrototypeConstants.AttackDamage,
            TpsCombatTestState.GetHp(TpsPrototypeConstants.MobObjectId)
        );
        var heart = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.NotifyUpdateHeart
        );
        var heartReader = new PacketReader(heart.Payload);
        Assert.Equal(TpsPrototypeConstants.MobObjectId, heartReader.ReadUInt());
        Assert.Equal(
            (uint)TpsPrototypeConstants.HeartsFromHp(hpBefore - TpsPrototypeConstants.AttackDamage),
            heartReader.ReadUInt()
        );
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyDisappearChara
        );
        Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.NotifyEmotionChara);
    }

    [Fact]
    public async Task AttackExec_FreeAimMiss_ConsumesTankWithoutHp()
    {
        TpsCombatTestState.ResetMonster(TpsPrototypeConstants.MobObjectId);
        var hpBefore = TpsCombatTestState.GetHp(TpsPrototypeConstants.MobObjectId);
        var (_, session, start, exec) = CreateBattleHandlers(42_4251, locked: false);
        var aimedHigh = MobSpawn with { Y = MobSpawn.Y + 500f };

        await start.HandleAsync(
            WriteVec3s(aimedHigh, PlayerSpawn),
            session,
            TestContext.Current.CancellationToken
        );
        await exec.HandleAsync(
            WriteVec3(PlayerSpawn),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyUpdateHitpoint
        );
        Assert.Contains(session.Sent, packet => packet.Type == PacketType.NotifyUpdateTank);
        Assert.Equal(hpBefore, TpsCombatTestState.GetHp(TpsPrototypeConstants.MobObjectId));
    }

    [Fact]
    public async Task AttackExec_LockHitsEvenIfAimMissesCylinder()
    {
        TpsCombatTestState.ResetMonster(TpsPrototypeConstants.MobObjectId);
        var hpBefore = TpsCombatTestState.GetHp(TpsPrototypeConstants.MobObjectId);
        var (_, session, start, exec) = CreateBattleHandlers(42_4252, locked: true);
        var aimedHigh = MobSpawn with { Y = MobSpawn.Y + 500f };

        await start.HandleAsync(
            WriteVec3s(aimedHigh, PlayerSpawn),
            session,
            TestContext.Current.CancellationToken
        );
        await exec.HandleAsync(
            WriteVec3(PlayerSpawn),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Contains(session.Sent, packet => packet.Type == PacketType.NotifyUpdateHitpoint);
        Assert.Equal(
            hpBefore - TpsPrototypeConstants.AttackDamage,
            TpsCombatTestState.GetHp(TpsPrototypeConstants.MobObjectId)
        );
    }

    [Fact]
    public void HeartsFromHp_MapsOneHeartPerShot()
    {
        Assert.Equal(5, TpsPrototypeConstants.HeartsFromHp(100));
        Assert.Equal(4, TpsPrototypeConstants.HeartsFromHp(80));
        Assert.Equal(1, TpsPrototypeConstants.HeartsFromHp(20));
        Assert.Equal(0, TpsPrototypeConstants.HeartsFromHp(0));
    }

    [Fact]
    public void BattleAttackStartRequest_ReadsTargetThenNow()
    {
        var parsed = BattleAttackStartRequest.FromBytes(WriteVec3s(MobSpawn, PlayerSpawn).Span);
        Assert.Equal(MobSpawn, parsed.TargetPos);
        Assert.Equal(PlayerSpawn, parsed.NowPos);
    }

    private static (
        SharedState State,
        CapturingPlayerSession Session,
        AreaBattleAttackStartHandler Start,
        AreaBattleAttackExecHandler Exec
    ) CreateBattleHandlers(uint characterId, bool locked)
    {
        var state = new SharedState();
        var session = new CapturingPlayerSession
        {
            CharacterId = characterId,
            LockedTargetId = locked ? TpsPrototypeConstants.MobObjectId : 0,
            X = PlayerSpawn.X,
            Y = PlayerSpawn.Y,
            Z = PlayerSpawn.Z,
        };
        state.RegisterClient(ServerType.Area, session);
        return (
            state,
            session,
            new AreaBattleAttackStartHandler(
                NullLogger<AreaBattleAttackStartHandler>.Instance,
                state
            ),
            new AreaBattleAttackExecHandler(NullLogger<AreaBattleAttackExecHandler>.Instance, state)
        );
    }

    private static ReadOnlyMemory<byte> WriteVec3(Vector3 value)
    {
        var writer = new PacketWriter();
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
        return writer.ToBytes();
    }

    private static ReadOnlyMemory<byte> WriteVec3s(Vector3 a, Vector3 b)
    {
        var writer = new PacketWriter();
        writer.Write(a.X);
        writer.Write(a.Y);
        writer.Write(a.Z);
        writer.Write(b.X);
        writer.Write(b.Y);
        writer.Write(b.Z);
        return writer.ToBytes();
    }
}
