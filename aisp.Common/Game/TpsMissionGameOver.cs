using System.Collections.Concurrent;
using aisp.Common.DAL.Entities;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

/// <summary>
/// Prototype mission end. A clear plays <c>tps_win_udx_{cosplay}.csv</c> and a fail plays
/// <c>tps_lose_udx_{cosplay}.csv</c>. Each script moves the avatar to the UDX stage, locks the camera,
/// plays the win or defeat motion, and shows the center graphic (effect 500 or 501). The result window
/// opens when that script reports it has finished.
/// The leave-mission confirm is opened by Escape, not here.
/// Mission action 7 tears down the TPS combat HUD (decomp <c>sub_4DEAA0</c> case 7).
/// Result dword 0 is fail, 1 is clear (<c>CTPSResultOpenCmd</c> / <c>sub_4F12A0</c>).
/// </summary>
public static class TpsMissionGameOver
{
    public const uint MissionActionEnd = 7;
    public const uint FailResultCode = 0;
    public const uint SuccessResultCode = 1;

    /// <summary>Prototype doll is cosplay 0 (素). The win script index is the cosplay id.</summary>
    public const uint PrototypeCosplayId = 0;

    private static readonly ConcurrentDictionary<Guid, byte[]> PendingClearResults = new();

    public static string WinScriptLabel(uint cosplayId = PrototypeCosplayId) =>
        $"./script/tps_event/tps_win_udx_{cosplayId}.csv";

    public static string LoseScriptLabel(uint cosplayId = PrototypeCosplayId) =>
        $"./script/tps_event/tps_lose_udx_{cosplayId}.csv";

    public static Task FailAsync(MissionInstance mission, ILogger logger, CancellationToken ct) =>
        FinishAsync(mission, logger, cleared: false, ct);

    public static Task ClearAsync(MissionInstance mission, ILogger logger, CancellationToken ct) =>
        FinishAsync(mission, logger, cleared: true, ct);

    public static void CancelPendingClear(Guid connectionId) =>
        PendingClearResults.TryRemove(connectionId, out _);

    /// <summary>
    /// Sends the stored clear or fail result after the ending script asks to finish. Returns false
    /// when this connection has no pending result.
    /// </summary>
    public static async Task<bool> TryPresentClearAsync(
        IPlayerSession session,
        CancellationToken ct
    )
    {
        if (!PendingClearResults.TryRemove(session.ConnectionId, out var result))
            return false;

        session.PendingEventEndAfterFade = false;
        session.ActiveEventKey = null;
        session.ActiveEventKind = NpcEventKind.None;
        session.ActiveEventCompletionPolicy = EventCompletionPolicy.Once;
        await session.SendAsync(PacketType.EventEndNotify, new EventEndNotify(0).ToBytes(), ct);
        await session.SendAsync(PacketType.NotifyMissionResultOpen, result, ct);
        return true;
    }

    public static MissionResultGrade GradeFor(bool cleared, TpsMissionStats stats)
    {
        if (!cleared)
            return MissionResultGrade.D;

        if (stats.Downs > 0)
            return MissionResultGrade.C;

        if (stats.Kills > 0 && stats.HitRatePercent >= 100)
            return MissionResultGrade.S;

        if (stats.HitRatePercent >= 80)
            return MissionResultGrade.A;

        if (stats.HitRatePercent >= 50)
            return MissionResultGrade.B;

        return MissionResultGrade.C;
    }

    private static async Task FinishAsync(
        MissionInstance mission,
        ILogger logger,
        bool cleared,
        CancellationToken ct
    )
    {
        if (!mission.TryMarkFinished())
            return;

        logger.LogInformation(
            cleared ? "TPS mission {MissionId} cleared" : "TPS mission {MissionId} failed",
            mission.Id
        );

        var now = Environment.TickCount64;
        foreach (var member in mission.Members)
        {
            var stats = member.Stats(now);
            var grade = GradeFor(cleared, stats);
            var result = new NotifyMissionResultOpen(
                cleared ? SuccessResultCode : FailResultCode,
                (uint)grade,
                member.Session.Character?.Name ?? "Player",
                stats.Downs,
                stats.Kills,
                stats.HitRatePercent,
                stats.ClearTimeSeconds
            ).ToBytes();

            if (!cleared)
            {
                foreach (var mob in mission.Mobs)
                {
                    mob.HitPoints = 0;
                    await member.Session.SendAsync(
                        PacketType.NotifyDisappearChara,
                        new NotifyDisappearChara(mob.ObjectId).ToBytes(),
                        ct
                    );
                }
            }

            var script = cleared ? WinScriptLabel() : LoseScriptLabel();
            await member.Session.SendAsync(
                PacketType.NotifyMissionAction,
                new NotifyMissionAction(MissionActionEnd).ToBytes(),
                ct
            );
            member.Session.ActiveEventKey = script;
            member.Session.ActiveEventKind = NpcEventKind.ClientScript;
            member.Session.ActiveEventCompletionPolicy = EventCompletionPolicy.Replayable;
            PendingClearResults[member.Session.ConnectionId] = result;
            await member.Session.SendAsync(
                PacketType.EventStartNotify,
                new EventStartNotify().ToBytes(),
                ct
            );
            await member.Session.SendAsync(
                PacketType.EventScriptPlayNotify,
                new EventScriptPlayNotify(script).ToBytes(),
                ct
            );
        }
    }
}
