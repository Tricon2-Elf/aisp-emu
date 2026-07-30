using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

/// <summary>
/// send_storage_close (0xB71B) → recv_storage_close_r (0x3D14).
/// Empty request; client waits on the response to dismiss the 倉庫 UI.
/// </summary>
public sealed class AreaStorageCloseHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.StorageCloseRequest;
    public PacketType ResponseType => PacketType.StorageCloseResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        await session.SendAsync(ResponseType, new StorageCloseResponse(0).ToBytes(), ct);
    }
}
