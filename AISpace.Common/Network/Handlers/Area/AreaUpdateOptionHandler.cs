using AISpace.Common.Network.Packets;

namespace AISpace.Common.Network.Handlers;

public class AreaUpdateOptionHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.UpdateOptionRequest;
    public PacketType ResponseType => PacketType.UpdateOptionResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var writer = new PacketWriter();
        writer.Write((uint)0);
        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
