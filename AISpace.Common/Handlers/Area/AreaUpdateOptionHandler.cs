using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaUpdateOptionHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.UpdateOptionRequest;
    public PacketType ResponseType => PacketType.UpdateOptionResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var writer = new PacketWriter();
        writer.Write((uint)0);
        await session.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
