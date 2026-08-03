using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

/// <summary>
/// send_storage_close (0xB71B) → recv_storage_close_r (0x3D14), and when opened from
/// My Room also recv_storage_furn_close_r (0x4E60).
/// </summary>
public sealed class AreaStorageCloseHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.StorageCloseRequest;
    public PacketType ResponseType => PacketType.StorageCloseResponse;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    ) => StorageSession.CloseAsync(session, ct);
}
