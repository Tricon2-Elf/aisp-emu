using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>Drama upload window closed: acknowledge so the client tears the window down.</summary>
public sealed class AreaAdventureUploadEndHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureUploadEndRequest;
    public PacketType ResponseType => PacketType.AdventureUploadEndResponse;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    ) => session.SendAsync(ResponseType, new AdventureUploadEndResponse().ToBytes(), ct);
}
