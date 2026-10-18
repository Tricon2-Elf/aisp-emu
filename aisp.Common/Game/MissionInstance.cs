using System.Numerics;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

public readonly record struct TpsPendingAim(Vector3 TargetPos, Vector3 NowPos);

public readonly record struct TpsMobMove(
    Vector3 From,
    Vector3 To,
    long StartTick,
    int DurationMs,
    int Yaw = 0,
    MovementType Gait = MovementType.Walking
);

public readonly record struct TpsMissionStats(
    uint Shots,
    uint Hits,
    uint Kills,
    uint Downs,
    uint ClearTimeSeconds
)
{
    public uint HitRatePercent => Shots == 0 ? 0 : Hits * 100 / Shots;
}

public enum MobAttackPhase
{
    Idle,
    Windup,
    Recover,
}

/// <summary>One enemy owned by a <see cref="MissionInstance"/>.</summary>
public sealed class MissionMob
{
    public uint ObjectId { get; init; }
    public int HitPoints { get; set; } = TpsPrototypeConstants.DefaultHitPoints;
    public Vector3 Position { get; set; }
    public TpsMobMove? Move { get; set; }
    public MobAttackPhase AttackPhase { get; set; }
    public long PhaseUntil { get; set; }
    public long NextAttackTick { get; set; }
    public long PauseUntil { get; set; }
    public uint AttackTargetId { get; set; }
    public Vector3 AttackTargetPos { get; set; }
    public bool Disappeared { get; set; }

    /// <summary>True after the client has been given a current position and a different destination.</summary>
    public bool HasSentPath { get; set; }

    public Vector3 PositionAt(long now)
    {
        if (Move is not { } move || move.DurationMs <= 0)
            return Position;

        var t = (now - move.StartTick) / (float)move.DurationMs;
        if (t <= 0f)
            return move.From;
        if (t >= 1f)
            return move.To;
        return Vector3.Lerp(move.From, move.To, t);
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;
        Move = null;
    }

    public void SetMove(
        Vector3 from,
        Vector3 to,
        int durationMs,
        long now,
        int yaw = 0,
        MovementType gait = MovementType.Walking
    )
    {
        Position = from;
        Move = new TpsMobMove(from, to, now, Math.Max(0, durationMs), yaw, gait);
    }
}

/// <summary>One player inside an active mission. Combat fields live here, not on the connection.</summary>
public sealed class MissionMember(IPlayerSession session)
{
    public IPlayerSession Session { get; } = session;
    public Guid ConnectionId => Session.ConnectionId;
    public uint CharacterId => Session.CharacterId;
    public float X { get; set; } = session.X;
    public float Y { get; set; } = session.Y;
    public float Z { get; set; } = session.Z;
    public int Rotation { get; set; } = session.Rotation;
    public uint LockedTargetId { get; set; }
    public float Stamina { get; set; } = TpsPrototypeConstants.StaminaMax;
    public bool IsSprinting { get; set; }
    public uint Tank { get; set; } = TpsPrototypeConstants.DefaultTank;
    public int HitPoints { get; set; } = TpsPrototypeConstants.DefaultHitPoints;
    public TpsPendingAim Aim { get; set; }
    public uint Shots { get; set; }
    public uint Hits { get; set; }
    public uint Kills { get; set; }
    public uint Downs { get; set; }
    public long StartTick { get; set; } = Environment.TickCount64;
    public long LastShotTick { get; set; }

    public Vector3 Position => new(X, Y, Z);

    public void PullFromSession()
    {
        X = Session.X;
        Y = Session.Y;
        Z = Session.Z;
        Rotation = Session.Rotation;
    }

    public TpsMissionStats Stats(long now)
    {
        var start = StartTick == 0 ? now : StartTick;
        var elapsed = (uint)Math.Max(0, (now - start) / 1000);
        return new TpsMissionStats(Shots, Hits, Kills, Downs, elapsed);
    }

    public bool TryAcceptShot(long now)
    {
        if (now - LastShotTick < 120)
            return false;

        LastShotTick = now;
        return true;
    }
}

/// <summary>
/// One active mission shared by up to four players. The connection only points at it.
/// A single loop drains stamina, moves mobs, and runs mob attacks.
/// </summary>
public sealed class MissionInstance
{
    public const int MaxMembers = 4;

    private readonly object _gate = new();
    private readonly List<MissionMember> _members = [];
    private readonly List<MissionMob> _mobs = [];
    private readonly List<DueItem> _due = [];
    private uint _nextMobId = TpsPrototypeConstants.MobObjectId;
    private CancellationTokenSource? _loop;
    private int _loopStarted;

    private enum DueKind
    {
        PlayerRecover,
        MobDisappear,
    }

    private sealed class DueItem(long dueTick, DueKind kind, uint subjectId)
    {
        public long DueTick { get; } = dueTick;
        public DueKind Kind { get; } = kind;
        public uint SubjectId { get; } = subjectId;
    }

    private readonly record struct Outbound(IPlayerSession Session, PacketType Type, byte[] Body);

    public MissionInstance(Guid id, uint mapId, uint leaderCharacterId)
    {
        Id = id;
        MapId = mapId;
        LeaderCharacterId = leaderCharacterId;
    }

    public Guid Id { get; }
    public uint MapId { get; }
    public uint LeaderCharacterId { get; }
    public bool Finished { get; private set; }

    public IReadOnlyList<MissionMember> Members
    {
        get
        {
            lock (_gate)
                return _members.ToArray();
        }
    }

    public IReadOnlyList<MissionMob> Mobs
    {
        get
        {
            lock (_gate)
                return _mobs.ToArray();
        }
    }

    public MissionMember? FindMember(uint characterId)
    {
        lock (_gate)
            return _members.FirstOrDefault(member => member.CharacterId == characterId);
    }

    public bool TryAddMember(IPlayerSession session)
    {
        lock (_gate)
        {
            if (Finished)
                return false;
            if (_members.Any(member => member.CharacterId == session.CharacterId))
                return true;
            if (_members.Count >= MaxMembers)
                return false;

            _members.Add(new MissionMember(session));
        }

        session.ActiveMissionId = Id;
        session.IsTpsMode = true;
        return true;
    }

    /// <summary>Returns true when the mission has no members left.</summary>
    public bool RemoveMember(uint characterId)
    {
        lock (_gate)
        {
            _members.RemoveAll(member => member.CharacterId == characterId);
            return _members.Count == 0;
        }
    }

    public MissionMob AddPrototypeMob(int hitPoints = TpsPrototypeConstants.DefaultHitPoints) =>
        AddMob(
            0,
            hitPoints,
            new Vector3(
                TpsPrototypeConstants.MobSpawnX,
                TpsPrototypeConstants.MobSpawnY,
                TpsPrototypeConstants.MobSpawnZ
            )
        );

    public MissionMob AddMob(uint objectId, int hitPoints, Vector3 position)
    {
        lock (_gate)
        {
            var id = objectId == 0 ? _nextMobId : objectId;
            _nextMobId = Math.Max(_nextMobId, id + 1);
            var mob = new MissionMob
            {
                ObjectId = id,
                HitPoints = hitPoints,
                Position = position,
            };
            _mobs.Add(mob);
            return mob;
        }
    }

    public void ClearMobs()
    {
        lock (_gate)
            _mobs.Clear();
    }

    public void SetAim(uint characterId, Vector3 targetPos, Vector3 nowPos)
    {
        lock (_gate)
        {
            var member = _members.FirstOrDefault(candidate => candidate.CharacterId == characterId);
            if (member is null)
                return;

            member.PullFromSession();
            member.Aim = new TpsPendingAim(targetPos, nowPos);
        }
    }

    public bool TryMarkFinished()
    {
        lock (_gate)
        {
            if (Finished)
                return false;
            Finished = true;
        }

        _loop?.Cancel();
        return true;
    }

    public void StartLoop(ILogger logger)
    {
        if (Interlocked.Exchange(ref _loopStarted, 1) != 0)
            return;

        _loop = new CancellationTokenSource();
        var token = _loop.Token;
        _ = RunLoopAsync(logger, token);
    }

    public void StopLoop()
    {
        _loop?.Cancel();
    }

    public Task BroadcastReportAsync(MissionMember member, uint actionType, CancellationToken ct) =>
        BroadcastReportAsync(member, actionType, TpsPrototypeConstants.DefaultSkills[0], ct);

    public async Task BroadcastReportAsync(
        MissionMember member,
        uint actionType,
        uint skillId,
        CancellationToken ct
    )
    {
        List<Outbound> outbound;
        lock (_gate)
        {
            member.PullFromSession();
            outbound = [];
            Fanout(outbound, BuildPlayerReport(member, actionType, skillId));
        }

        await SendAllAsync(outbound, ct);
    }

    public async Task ApplyPlayerShotAsync(
        ILogger logger,
        MissionMember member,
        Vector3 execNowPos,
        CancellationToken ct
    )
    {
        List<Outbound> outbound;
        var clear = false;
        lock (_gate)
        {
            if (Finished)
                return;

            var now = Environment.TickCount64;
            member.PullFromSession();
            if (!member.TryAcceptShot(now))
                return;

            member.Tank =
                member.Tank >= TpsPrototypeConstants.TankConsumePerShot
                    ? member.Tank - TpsPrototypeConstants.TankConsumePerShot
                    : 0;
            outbound = [];
            Fanout(
                outbound,
                (
                    PacketType.NotifyUpdateTank,
                    new NotifyUpdateTank(member.CharacterId, member.Tank).ToBytes()
                )
            );

            var mob = ResolveTargetMob(member);
            var locked = member.LockedTargetId != 0;
            var (origin, targetPos) = ResolveAim(member, execNowPos);
            if (!locked && (mob is null || !TpsAimHitTest.HitsMob(origin, targetPos, mob, now)))
            {
                member.Shots++;
                var mobPos = mob?.PositionAt(now) ?? default;
                logger.LogInformation(
                    "Shot missed Monster {TargetId} (free aim). Tank: {Tank}% origin=({Ox:0},{Oy:0},{Oz:0}) target=({Tx:0},{Ty:0},{Tz:0}) mob=({Mx:0},{My:0},{Mz:0})",
                    mob?.ObjectId ?? 0,
                    member.Tank,
                    origin.X,
                    origin.Y,
                    origin.Z,
                    targetPos.X,
                    targetPos.Y,
                    targetPos.Z,
                    mobPos.X,
                    mobPos.Y,
                    mobPos.Z
                );
            }
            else if (mob is null)
            {
                member.Shots++;
            }
            else
            {
                member.Shots++;
                member.Hits++;
                var died = DamageMob(mob, TpsPrototypeConstants.AttackDamage);
                if (died)
                    member.Kills++;

                var remHearts = TpsPrototypeConstants.HeartsFromHp(mob.HitPoints);
                logger.LogInformation(
                    "Shot hit Monster {TargetId}! HP: {Hp}/100, Hearts: {Hearts}/5, Tank: {Tank}%",
                    mob.ObjectId,
                    mob.HitPoints,
                    remHearts,
                    member.Tank
                );
                Fanout(
                    outbound,
                    (
                        PacketType.NotifyUpdateHitpoint,
                        new NotifyUpdateHitpoint(mob.ObjectId, (uint)mob.HitPoints).ToBytes()
                    )
                );
                Fanout(
                    outbound,
                    (
                        PacketType.NotifyUpdateHeart,
                        new NotifyUpdateHeart(mob.ObjectId, (uint)remHearts).ToBytes()
                    )
                );

                if (died)
                {
                    logger.LogInformation(
                        "Monster {TargetId} DEFEATED! Total kills: {Kills}",
                        mob.ObjectId,
                        member.Kills
                    );
                    Fanout(
                        outbound,
                        (
                            PacketType.NotifyEmotionChara,
                            new NotifyEmotionChara(mob.ObjectId, 3).ToBytes()
                        )
                    );
                    Fanout(
                        outbound,
                        (
                            PacketType.NotifyMissionAction,
                            new NotifyMissionAction(2, member.Kills).ToBytes()
                        )
                    );
                    member.LockedTargetId = 0;
                    Fanout(
                        outbound,
                        (
                            PacketType.NotifyBattleTargetUnlock,
                            new NotifyBattleTargetUnlock(member.CharacterId).ToBytes()
                        )
                    );
                    _due.Add(new DueItem(now + 1000, DueKind.MobDisappear, mob.ObjectId));
                    clear = AllMobsDefeated();
                }
            }

            _due.Add(
                new DueItem(
                    now + TpsPrototypeConstants.ShotRecoverDelayMs,
                    DueKind.PlayerRecover,
                    member.CharacterId
                )
            );
        }

        await SendAllAsync(outbound, ct);
        if (clear)
            await TpsMissionGameOver.ClearAsync(this, logger, ct);
    }

    public async Task TickAsync(ILogger logger, CancellationToken ct = default)
    {
        List<Outbound> outbound;
        uint? failCharacterId = null;
        lock (_gate)
        {
            if (Finished)
                return;

            var now = Environment.TickCount64;
            outbound = [];
            foreach (var member in _members)
            {
                member.PullFromSession();
                TickStamina(outbound, member);
            }

            foreach (var mob in _mobs)
                TickMob(outbound, mob, now, ref failCharacterId);

            for (var i = _due.Count - 1; i >= 0; i--)
            {
                if (_due[i].DueTick > now)
                    continue;

                var due = _due[i];
                _due.RemoveAt(i);
                ApplyDue(outbound, due);
            }
        }

        await SendAllAsync(outbound, ct);
        if (failCharacterId is uint fallen)
            await TpsMissionGameOver.FailAsync(this, logger, fallen, ct);
    }

    private async Task RunLoopAsync(ILogger logger, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var empty = false;
                lock (_gate)
                    empty = Finished || _members.Count == 0;
                if (empty)
                    break;

                await TickAsync(logger, CancellationToken.None);
                await Task.Delay(TpsPrototypeConstants.StaminaTickMs, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // mission finished or the last member left
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mission {MissionId} loop stopped", Id);
        }
    }

    private void TickStamina(List<Outbound> outbound, MissionMember member)
    {
        if (!member.IsSprinting && member.Stamina >= TpsPrototypeConstants.StaminaMax)
            return;

        var sprinting = member.IsSprinting;
        member.Stamina = Math.Clamp(
            member.Stamina
                + (
                    sprinting
                        ? -TpsPrototypeConstants.StaminaDrainPerTick
                        : TpsPrototypeConstants.StaminaRegenPerTick
                ),
            0f,
            TpsPrototypeConstants.StaminaMax
        );
        Fanout(
            outbound,
            (
                PacketType.NotifyUpdateStamina,
                new NotifyUpdateStamina(
                    member.CharacterId,
                    member.Stamina,
                    sprinting
                        ? TpsPrototypeConstants.StaminaDrainSpeed
                        : TpsPrototypeConstants.StaminaRegenSpeed
                ).ToBytes()
            )
        );

        if (sprinting && member.Stamina <= 0f)
        {
            member.IsSprinting = false;
            Fanout(
                outbound,
                BuildPlayerReport(
                    member,
                    TpsPrototypeConstants.BattleReportDashEndAction,
                    TpsPrototypeConstants.BattleReportDashEndSkillId
                )
            );
        }
    }

    private void TickMob(
        List<Outbound> outbound,
        MissionMob mob,
        long now,
        ref uint? failCharacterId
    )
    {
        if (mob.HitPoints <= 0 || mob.Disappeared)
            return;

        if (mob.Move is { } move && now >= move.StartTick + move.DurationMs)
        {
            mob.Position = move.To;
            mob.Move = null;
            mob.PauseUntil =
                now
                + Random.Shared.Next(
                    TpsPrototypeConstants.MobWanderPauseMinMs,
                    TpsPrototypeConstants.MobWanderPauseMaxMs
                );
            // Do not send a stop here. A path with no separate destination is read as the origin.
        }

        var current = mob.PositionAt(now);
        var target = FirstLivingMemberInRange(current);

        if (mob.AttackPhase == MobAttackPhase.Windup && now >= mob.PhaseUntil)
        {
            if (target is null || Finished)
            {
                mob.AttackPhase = MobAttackPhase.Idle;
            }
            else
            {
                Fanout(
                    outbound,
                    BuildMobReport(mob, TpsPrototypeConstants.BattleReportShotAction, target)
                );
                var died = DamageMember(target, TpsPrototypeConstants.MobAttackDamage);
                var hearts = TpsPrototypeConstants.HeartsFromHp(target.HitPoints);
                Fanout(
                    outbound,
                    (
                        PacketType.NotifyUpdateHitpoint,
                        new NotifyUpdateHitpoint(
                            target.CharacterId,
                            (uint)target.HitPoints
                        ).ToBytes()
                    )
                );
                Fanout(
                    outbound,
                    (
                        PacketType.NotifyUpdateHeart,
                        new NotifyUpdateHeart(target.CharacterId, (uint)hearts).ToBytes()
                    )
                );
                mob.AttackPhase = MobAttackPhase.Recover;
                mob.PhaseUntil = now + TpsPrototypeConstants.ShotRecoverDelayMs;
                if (died)
                    failCharacterId = target.CharacterId;
            }

            return;
        }

        if (mob.AttackPhase == MobAttackPhase.Recover && now >= mob.PhaseUntil)
        {
            if (target is not null)
            {
                Fanout(
                    outbound,
                    BuildMobReport(mob, TpsPrototypeConstants.BattleReportRecoverAction, target)
                );
            }

            mob.AttackPhase = MobAttackPhase.Idle;
            mob.NextAttackTick = now + TpsPrototypeConstants.MobAttackIntervalMs;
            return;
        }

        if (mob.AttackPhase != MobAttackPhase.Idle)
            return;

        if (mob.Move is not null || now < mob.PauseUntil)
            return;

        // The first move has to be a real current→new path. A stop, or one sample,
        // is what makes the client put the mob at the origin.
        if (!mob.HasSentPath)
        {
            BeginWander(outbound, mob, current, now);
            return;
        }

        if (target is not null && now >= mob.NextAttackTick)
        {
            mob.Position = current;
            mob.Move = null;
            mob.AttackPhase = MobAttackPhase.Windup;
            mob.PhaseUntil = now + TpsPrototypeConstants.MobAttackWindupMs;
            mob.AttackTargetId = target.CharacterId;
            mob.AttackTargetPos = target.Position;
            Fanout(
                outbound,
                BuildMobReport(mob, TpsPrototypeConstants.BattleReportAttackAction, target)
            );
            return;
        }

        BeginWander(outbound, mob, current, now);
    }

    /// <summary>
    /// Current position, then the destination. The client uses a missing first sample as the origin.
    /// </summary>
    private void BeginWander(List<Outbound> outbound, MissionMob mob, Vector3 current, long now)
    {
        var dest = NextWanderPoint(current);
        var dx = dest.X - current.X;
        var dz = dest.Z - current.Z;
        var walkYaw = TpsMobCombat.YawToward(dx, dz);
        var gait = Random.Shared.Next(2) == 0 ? MovementType.Walking : MovementType.Running;
        var dist = MathF.Sqrt(dx * dx + dz * dz);
        var speed =
            gait == MovementType.Running
                ? TpsPrototypeConstants.MobWanderRunSpeed
                : TpsPrototypeConstants.MobWanderWalkSpeed;
        var durationMs = (int)Math.Clamp(dist / Math.Max(speed, 1f) * 1000f, 1d, 30_000d);
        mob.SetMove(current, dest, durationMs, now, walkYaw, gait);
        mob.HasSentPath = true;
        // Animation 6 stores the destination on the enemy controller. The walk
        // samples are what the controller follows; without the target sample it follows the origin.
        Fanout(
            outbound,
            (
                PacketType.AvatarNotifyMove,
                new AvatarNotifyMove(
                    mob.ObjectId,
                    [
                        new MovementData(dest.X, dest.Y, dest.Z, walkYaw, MovementType.SetTarget),
                        new MovementData(current.X, current.Y, current.Z, walkYaw, gait),
                        new MovementData(dest.X, dest.Y, dest.Z, walkYaw, gait),
                    ]
                ).ToBytes()
            )
        );
    }

    private void ApplyDue(List<Outbound> outbound, DueItem due)
    {
        if (due.Kind == DueKind.MobDisappear)
        {
            var mob = _mobs.FirstOrDefault(candidate => candidate.ObjectId == due.SubjectId);
            if (mob is not null)
                mob.Disappeared = true;
            Fanout(
                outbound,
                (PacketType.NotifyDisappearChara, new NotifyDisappearChara(due.SubjectId).ToBytes())
            );
            return;
        }

        var member = _members.FirstOrDefault(candidate => candidate.CharacterId == due.SubjectId);
        if (member is null || Finished)
            return;

        Fanout(
            outbound,
            BuildPlayerReport(
                member,
                TpsPrototypeConstants.BattleReportRecoverAction,
                TpsPrototypeConstants.DefaultSkills[0]
            )
        );
    }

    private (PacketType Type, byte[] Body) BuildPlayerReport(
        MissionMember member,
        uint actionType,
        uint skillId
    )
    {
        var useTargetPos =
            member.LockedTargetId == 0
            && (
                actionType == TpsPrototypeConstants.BattleReportAttackAction
                || actionType == TpsPrototypeConstants.BattleReportShotAction
            );
        if (useTargetPos)
        {
            return (
                PacketType.NotifyBattleReportTargetPos,
                new NotifyBattleReportTargetPos(
                    member.CharacterId,
                    actionType,
                    0,
                    skillId,
                    member.Aim.TargetPos
                ).ToBytes()
            );
        }

        var targetId = member.LockedTargetId != 0 ? member.LockedTargetId : FirstMobId();
        return (
            PacketType.NotifyBattleReportTargetObj,
            new NotifyBattleReportTargetObj(
                member.CharacterId,
                actionType,
                0,
                skillId,
                targetId
            ).ToBytes()
        );
    }

    private static (PacketType Type, byte[] Body) BuildMobReport(
        MissionMob mob,
        uint actionType,
        MissionMember target
    )
    {
        var skillId = TpsPrototypeConstants.DefaultSkills[0];
        var usePos =
            actionType == TpsPrototypeConstants.BattleReportAttackAction
            || actionType == TpsPrototypeConstants.BattleReportShotAction;
        if (usePos)
        {
            return (
                PacketType.NotifyBattleReportTargetPos,
                new NotifyBattleReportTargetPos(
                    mob.ObjectId,
                    actionType,
                    0,
                    skillId,
                    target.Position
                ).ToBytes()
            );
        }

        return (
            PacketType.NotifyBattleReportTargetObj,
            new NotifyBattleReportTargetObj(
                mob.ObjectId,
                actionType,
                0,
                skillId,
                target.CharacterId
            ).ToBytes()
        );
    }

    private MissionMob? ResolveTargetMob(MissionMember member)
    {
        if (member.LockedTargetId != 0)
        {
            var locked = _mobs.FirstOrDefault(mob => mob.ObjectId == member.LockedTargetId);
            if (locked is not null)
                return locked;
        }

        return _mobs.FirstOrDefault(mob => mob.HitPoints > 0) ?? _mobs.FirstOrDefault();
    }

    private static (Vector3 Origin, Vector3 TargetPos) ResolveAim(
        MissionMember member,
        Vector3 execNowPos
    )
    {
        var origin =
            execNowPos != Vector3.Zero ? execNowPos
            : member.Aim.NowPos != Vector3.Zero ? member.Aim.NowPos
            : member.Position;
        return (origin, member.Aim.TargetPos);
    }

    private static bool DamageMob(MissionMob mob, int damage)
    {
        if (mob.HitPoints <= 0)
            return false;

        mob.HitPoints = Math.Max(0, mob.HitPoints - damage);
        return mob.HitPoints == 0;
    }

    private static bool DamageMember(MissionMember member, int damage)
    {
        if (member.HitPoints <= 0)
            return false;

        member.HitPoints = Math.Max(0, member.HitPoints - damage);
        if (member.HitPoints > 0)
            return false;

        member.Downs++;
        return true;
    }

    private bool AllMobsDefeated() => _mobs.Count > 0 && _mobs.All(mob => mob.HitPoints <= 0);

    private uint FirstMobId() =>
        _mobs.FirstOrDefault()?.ObjectId ?? TpsPrototypeConstants.MobObjectId;

    private MissionMember? FirstLivingMemberInRange(Vector3 mobPosition)
    {
        foreach (var member in _members)
        {
            if (member.HitPoints <= 0)
                continue;
            if (TpsMobCombat.IsPlayerInAttackRange(mobPosition, member.Position))
                return member;
        }

        return null;
    }

    private void Fanout(List<Outbound> outbound, (PacketType Type, byte[] Body) packet)
    {
        foreach (var member in _members)
            outbound.Add(new Outbound(member.Session, packet.Type, packet.Body));
    }

    private static async Task SendAllAsync(List<Outbound> outbound, CancellationToken ct)
    {
        foreach (var packet in outbound)
        {
            try
            {
                await packet.Session.SendAsync(packet.Type, packet.Body, ct);
            }
            catch (Exception)
            {
                // one member dropping must not stop the mission tick
            }
        }
    }

    private static Vector3 NextWanderPoint(Vector3 current)
    {
        var spawn = new Vector3(
            TpsPrototypeConstants.MobSpawnX,
            TpsPrototypeConstants.MobSpawnY,
            TpsPrototypeConstants.MobSpawnZ
        );
        var radius = TpsPrototypeConstants.MobWanderRadius;
        var radiusSq = radius * radius;
        var rng = Random.Shared;

        for (var i = 0; i < 8; i++)
        {
            var heading = rng.NextSingle() * MathF.Tau;
            var step =
                TpsPrototypeConstants.MobWanderStepMin
                + rng.NextSingle()
                    * (
                        TpsPrototypeConstants.MobWanderStepMax
                        - TpsPrototypeConstants.MobWanderStepMin
                    );
            var candidate = new Vector3(
                current.X + MathF.Sin(heading) * step,
                TpsPrototypeConstants.MobSpawnY,
                current.Z + MathF.Cos(heading) * step
            );
            var ox = candidate.X - spawn.X;
            var oz = candidate.Z - spawn.Z;
            if (ox * ox + oz * oz <= radiusSq)
                return candidate;
        }

        return spawn;
    }
}
