using AISpace.Common.DAL.Entities;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Game;

public static class ClientScriptLauncher
{
    public static async Task StartAsync(IPlayerSession session, string eventKey, bool persistCompletion = true, CancellationToken ct = default)
    {
        if (persistCompletion)
        {
            session.ActiveEventKey = eventKey;
            session.ActiveEventKind = NpcEventKind.ClientScript;
        }

        await session.SendAsync(PacketType.EventStartNotify, new EventStartNotify().ToBytes(), ct);
        await session.SendAsync(PacketType.EventScriptPlayNotify, new EventScriptPlayNotify(ScriptedEvents.GetScriptLabel(eventKey)).ToBytes(), ct);
    }
}
