using AISpace.Common.DAL.Entities;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Game.ServerScripts;

public sealed class ServerScriptDispatcher(IEnumerable<IServerScript> scripts, ServerScriptSession serverScriptSession, ILogger<ServerScriptDispatcher> logger)
{
    private readonly IReadOnlyDictionary<string, IServerScript> _scripts = scripts.ToDictionary(script => script.EventKey, StringComparer.Ordinal);

    public bool HasScript(string eventKey) => _scripts.ContainsKey(eventKey);

    public EventCompletionPolicy GetCompletionPolicy(string eventKey) => _scripts.TryGetValue(eventKey, out var script) ? script.CompletionPolicy : EventCompletionPolicy.Once;

    public async Task StartAsync(IPlayerSession session, string eventKey, ServerScriptContext context, EventCompletionPolicy completionPolicy, CancellationToken ct = default)
    {
        if (!_scripts.TryGetValue(eventKey, out var script))
        {
            logger.LogWarning("Unknown server script {EventKey} for character {CharacterId}", eventKey, session.CharacterId);
            return;
        }

        serverScriptSession.Begin(session, eventKey, completionPolicy);
        await session.SendAsync(PacketType.EventStartNotify, new EventStartNotify().ToBytes(), ct);
        await script.StartAsync(session, context, ct);
    }

    public async Task<bool> TryHandlePacketAsync(PacketType packetType, ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (session.ActiveEventKind != NpcEventKind.ServerScript || session.ActiveEventKey is null)
            return false;

        if (!_scripts.TryGetValue(session.ActiveEventKey, out var script))
            return false;

        return await script.TryHandlePacketAsync(packetType, payload, session, ct);
    }
}
