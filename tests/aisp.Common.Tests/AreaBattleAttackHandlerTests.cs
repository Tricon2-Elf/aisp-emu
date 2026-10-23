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
        var (missions, mission, session) = CreateMission(42_4242, locked: true);
        var handler = new AreaBattleAttackStartHandler(
            NullLogger<AreaBattleAttackStartHandler>.Instance,
            missions
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
        var (missions, _, session) = CreateMission(42_4243, locked: true);
        var handler = new AreaBattleAttackExecHandler(
            NullLogger<AreaBattleAttackExecHandler>.Instance,
            missions
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
        var (missions, mission, session) = CreateMission(42_4253, locked: false);
        var handler = new AreaBattleAttackBlazeHandler(
            NullLogger<AreaBattleAttackBlazeHandler>.Instance,
            missions
        );
        await handler.HandleAsync(
            WriteVec3s(MobSpawn, PlayerSpawn),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Single(session.Sent);
        Assert.Equal(PacketType.BattleAttackBlazeResponse, session.Sent[0].Type);
        Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());
        var aim = mission.FindMember(session.CharacterId)!.Aim;
        Assert.Equal(MobSpawn, aim.TargetPos);
        Assert.Equal(PlayerSpawn, aim.NowPos);
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
        var (missions, mission, session) = CreateMission(42_4245, locked: false);
        var handler = new AreaBattleDashExecHandler(
            NullLogger<AreaBattleDashExecHandler>.Instance,
            missions
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
        Assert.Equal((byte)0, reader.ReadByte());
        Assert.Equal(TpsPrototypeConstants.BattleReportDashSkillId, reader.ReadUInt());
        await mission.TickAsync(NullLogger.Instance, TestContext.Current.CancellationToken);
        Assert.Contains(session.Sent, packet => packet.Type == PacketType.NotifyUpdateStamina);
        var member = mission.FindMember(session.CharacterId)!;
        Assert.True(member.IsSprinting);
        member.IsSprinting = false;
        member.Stamina = TpsPrototypeConstants.StaminaMax;
    }

    [Fact]
    public async Task DashExec_RejectsWhenStaminaIsNotFull()
    {
        var (missions, mission, session) = CreateMission(42_4246, locked: false);
        mission.FindMember(session.CharacterId)!.Stamina = 50f;

        var handler = new AreaBattleDashExecHandler(
            NullLogger<AreaBattleDashExecHandler>.Instance,
            missions
        );
        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1u, new PacketReader(session.Sent[0].Payload).ReadUInt());
        var report = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.NotifyBattleReportTargetObj
        );
        var reader = new PacketReader(report.Payload);
        reader.ReadUInt();
        Assert.Equal(TpsPrototypeConstants.BattleReportDashEndAction, reader.ReadUInt());
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyUpdateStamina
        );
    }

    [Fact]
    public async Task DashFinish_AcksAndSendsDashEndReport()
    {
        var (missions, _, session) = CreateMission(42_4244, locked: true);
        var handler = new AreaBattleDashFinishHandler(
            NullLogger<AreaBattleDashFinishHandler>.Instance,
            missions
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
        var (_, mission, session, start, exec) = CreateBattleHandlers(42_4250, locked: false);
        var hpBefore = mission.Mobs[0].HitPoints;

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
        Assert.Equal(hpBefore - TpsPrototypeConstants.AttackDamage, mission.Mobs[0].HitPoints);
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
        var (_, mission, session, start, exec) = CreateBattleHandlers(42_4251, locked: false);
        var hpBefore = mission.Mobs[0].HitPoints;
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
        Assert.Equal(hpBefore, mission.Mobs[0].HitPoints);
    }

    [Fact]
    public async Task AttackExec_LockHitsEvenIfAimMissesCylinder()
    {
        var (_, mission, session, start, exec) = CreateBattleHandlers(42_4252, locked: true);
        var hpBefore = mission.Mobs[0].HitPoints;
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
        Assert.Equal(hpBefore - TpsPrototypeConstants.AttackDamage, mission.Mobs[0].HitPoints);
    }

    [Fact]
    public async Task AttackExec_LastOwnedMob_OpensSuccessResults()
    {
        const uint mobId = 2000399;
        var (_, mission, session, start, exec) = CreateBattleHandlers(42_4399, locked: true);
        mission.ClearMobs();
        var mob = mission.AddMob(mobId, TpsPrototypeConstants.AttackDamage, MobSpawn);
        mission.FindMember(session.CharacterId)!.LockedTargetId = mobId;

        await start.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );
        await exec.HandleAsync(
            WriteVec3(PlayerSpawn),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, mob.HitPoints);
        Assert.All(mission.Mobs, candidate => Assert.True(candidate.HitPoints <= 0));
        Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventScriptPlayNotify);
        Assert.True(
            await TpsMissionGameOver.TryPresentClearAsync(
                session,
                TestContext.Current.CancellationToken
            )
        );
        var result = Assert.Single(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionResultOpen
        );
        Assert.Equal(
            TpsMissionGameOver.SuccessResultCode,
            new PacketReader(result.Payload).ReadUInt()
        );
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.NotifyMissionSituationMessage
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
        MissionRegistry Missions,
        MissionInstance Mission,
        CapturingPlayerSession Session
    ) CreateMission(uint characterId, bool locked)
    {
        var missions = new MissionRegistry();
        var session = new CapturingPlayerSession
        {
            CharacterId = characterId,
            X = PlayerSpawn.X,
            Y = PlayerSpawn.Y,
            Z = PlayerSpawn.Z,
        };
        var mission = missions.Create(session);
        mission.AddPrototypeMob();
        if (locked)
            mission.FindMember(characterId)!.LockedTargetId = TpsPrototypeConstants.MobObjectId;
        return (missions, mission, session);
    }

    private static (
        MissionRegistry Missions,
        MissionInstance Mission,
        CapturingPlayerSession Session,
        AreaBattleAttackStartHandler Start,
        AreaBattleAttackExecHandler Exec
    ) CreateBattleHandlers(uint characterId, bool locked)
    {
        var (missions, mission, session) = CreateMission(characterId, locked);
        return (
            missions,
            mission,
            session,
            new AreaBattleAttackStartHandler(
                NullLogger<AreaBattleAttackStartHandler>.Instance,
                missions
            ),
            new AreaBattleAttackExecHandler(
                NullLogger<AreaBattleAttackExecHandler>.Instance,
                missions
            )
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
