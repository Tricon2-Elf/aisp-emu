using aisp.Common.Config;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

public static class MotdNotice
{
    public static async Task TrySendPendingAsync(
        IPlayerSession session,
        SharedState state,
        MotdOptions? options,
        ILogger logger,
        CancellationToken ct = default
    )
    {
        if (!session.NeedsMotd)
            return;

        session.NeedsMotd = false;
        if (options?.TryGetMessage(session.Language, out var motd) != true)
            return;

        // recv_talk_forward is a Msg opcode (System / Notice chat filter). Prefer the Msg
        // connection so the System Message box actually receives it; fall back to Area.
        var target = state.GetMsgSessionByUserId(session.UserId) ?? session;
        try
        {
            await SystemNotice.SendAsync(target, motd, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed sending MOTD for user {UserId}", session.UserId);
        }
    }
}
