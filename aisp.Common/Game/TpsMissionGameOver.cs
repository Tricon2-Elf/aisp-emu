using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

/// <summary>
/// Prototype mission end: situation banner, then the result window.
/// The leave-mission confirm is opened by Escape, not here, so a clear does not show it.
/// Mission action 7 tears down the TPS combat HUD (decomp <c>sub_4DEAA0</c> case 7).
/// Result dword 0 is fail, 1 is clear (<c>CTPSResultOpenCmd</c> / <c>sub_4F12A0</c>).
/// </summary>
public static class TpsMissionGameOver
{
    public const uint MissionActionEnd = 7;
    public const uint FailResultCode = 0;
    public const uint SuccessResultCode = 1;
    public const string FailMessage = "ミッション失敗！！";
    public const string SuccessMessage = "ミッション成功！！";

    public static Task FailAsync(
        MissionInstance mission,
        ILogger logger,
        uint emotionCharacterId,
        CancellationToken ct
    ) => FinishAsync(mission, logger, emotionCharacterId, cleared: false, ct);

    public static Task ClearAsync(MissionInstance mission, ILogger logger, CancellationToken ct) =>
        FinishAsync(mission, logger, emotionCharacterId: 0, cleared: true, ct);

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
        uint emotionCharacterId,
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

                await member.Session.SendAsync(
                    PacketType.NotifyEmotionChara,
                    new NotifyEmotionChara(emotionCharacterId, 3).ToBytes(),
                    ct
                );
            }

            var stats = member.Stats(now);
            var grade = GradeFor(cleared, stats);
            await member.Session.SendAsync(
                PacketType.NotifyMissionAction,
                new NotifyMissionAction(MissionActionEnd).ToBytes(),
                ct
            );
            await member.Session.SendAsync(
                PacketType.NotifyMissionSituationMessage,
                new NotifyMissionSituationMessage(cleared ? SuccessMessage : FailMessage).ToBytes(),
                ct
            );
            await member.Session.SendAsync(
                PacketType.NotifyMissionResultOpen,
                new NotifyMissionResultOpen(
                    cleared ? SuccessResultCode : FailResultCode,
                    (uint)grade,
                    member.Session.Character?.Name ?? "Player",
                    stats.Downs,
                    stats.Kills,
                    stats.HitRatePercent,
                    stats.ClearTimeSeconds
                ).ToBytes(),
                ct
            );
        }
    }
}
