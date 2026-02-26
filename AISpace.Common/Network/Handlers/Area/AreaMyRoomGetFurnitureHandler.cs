namespace AISpace.Common.Network.Handlers;

public class AreaMyRoomGetFurnitureHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MyRoomGetFurnitureRequest;
    public PacketType ResponseType => PacketType.MyRoomGetFurnitureResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); 
        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
