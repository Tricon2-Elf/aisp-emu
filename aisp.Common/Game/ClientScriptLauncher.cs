using aisp.Common.DAL.Entities;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Game;

public static class ClientScriptLauncher
{
    public static async Task StartAsync(
        IPlayerSession session,
        string eventKey,
        EventCompletionPolicy completionPolicy,
        CancellationToken ct = default
    )
    {
        session.ActiveEventKey = eventKey;
        session.ActiveEventKind = NpcEventKind.ClientScript;
        session.ActiveEventCompletionPolicy = completionPolicy;

        await session.SendAsync(PacketType.EventStartNotify, new EventStartNotify().ToBytes(), ct);
        await session.SendAsync(
            PacketType.EventScriptPlayNotify,
            new EventScriptPlayNotify(ScriptedEvents.GetScriptLabel(eventKey)).ToBytes(),
            ct
        );
    }
}
