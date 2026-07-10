using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Game;

public static class ScriptedEventLauncher
{
    public static async Task StartAsync(IPlayerSession session, string eventKey, CancellationToken ct = default)
    {
        session.ActiveScriptedEventKey = eventKey;
        await session.SendAsync(PacketType.EventStartNotify, new EventStartNotify().ToBytes(), ct);
        await session.SendAsync(PacketType.EventScriptPlayNotify, new EventScriptPlayNotify(ScriptedEvents.GetScriptLabel(eventKey)).ToBytes(), ct);
    }
}
