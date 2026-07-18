using AISpace.Common.DAL.Entities;
using AISpace.Network;

namespace AISpace.Common.Game.ServerScripts;

public interface IServerScript
{
    string EventKey { get; }
    EventCompletionPolicy CompletionPolicy => EventCompletionPolicy.Once;
    Task StartAsync(IPlayerSession session, ServerScriptContext context, CancellationToken ct = default);
    Task<bool> TryHandlePacketAsync(PacketType packetType, ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default);
}
