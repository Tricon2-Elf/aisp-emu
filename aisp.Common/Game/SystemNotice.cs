using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Game;

public static class SystemNotice
{
    // DistID -5 is the client "System" / Notice chat filter (see sub_428B10 / sub_428BB0).
    public const uint DistId = unchecked((uint)-5);

    public static Task SendAsync(
        IPlayerSession session,
        string text,
        CancellationToken ct = default
    ) =>
        session.SendAsync(
            PacketType.TalkForwardNotify,
            new TalkForwardNotify(0, DistId, $"{text}\r\n", 0).ToBytes(),
            ct
        );
}
