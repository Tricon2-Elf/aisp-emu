using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

/// <summary>
/// Prototype mission end: situation banner, result window, then the out-map confirm.
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

    public static async Task SendAsync(IPlayerSession session, ILogger logger, CancellationToken ct)
    {
        if (!TpsCombatTestState.TryFinishMission(session.CharacterId))
            return;

        logger.LogInformation("TPS game over for character {CharacterId}", session.CharacterId);

        foreach (var mobObjId in TpsCombatTestState.DeactivateMobsFor(session.CharacterId))
        {
            await session.SendAsync(
                PacketType.NotifyDisappearChara,
                new NotifyDisappearChara(mobObjId).ToBytes(),
                ct
            );
        }

        await session.SendAsync(
            PacketType.NotifyEmotionChara,
            new NotifyEmotionChara(session.CharacterId, 3).ToBytes(),
            ct
        );
        await SendResultUiAsync(session, FailMessage, FailResultCode, cleared: false, ct);
    }

    public static async Task SendSuccessAsync(
        IPlayerSession session,
        ILogger logger,
        CancellationToken ct
    )
    {
        if (!TpsCombatTestState.TryFinishMission(session.CharacterId))
            return;

        logger.LogInformation("TPS mission clear for character {CharacterId}", session.CharacterId);
        await SendResultUiAsync(session, SuccessMessage, SuccessResultCode, cleared: true, ct);
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

    private static async Task SendResultUiAsync(
        IPlayerSession session,
        string message,
        uint resultCode,
        bool cleared,
        CancellationToken ct
    )
    {
        var stats = TpsCombatTestState.GetMissionStats(session.CharacterId);
        var grade = GradeFor(cleared, stats);

        await session.SendAsync(
            PacketType.NotifyMissionAction,
            new NotifyMissionAction(MissionActionEnd).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.NotifyMissionSituationMessage,
            new NotifyMissionSituationMessage(message).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.NotifyMissionResultOpen,
            new NotifyMissionResultOpen(
                resultCode,
                (uint)grade,
                session.Character?.Name ?? "Player",
                stats.Downs,
                stats.Kills,
                stats.HitRatePercent,
                stats.ClearTimeSeconds
            ).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.NotifyMissionOutmapChoiceOpen,
            new NotifyMissionOutmapChoiceOpen().ToBytes(),
            ct
        );
    }
}
