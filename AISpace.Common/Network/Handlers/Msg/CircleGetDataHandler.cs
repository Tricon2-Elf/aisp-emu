namespace AISpace.Common.Network.Handlers.Msg;

public class CircleGetDataHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.CircleGetDataRequest;

    public PacketType ResponseType => PacketType.CircleGetDataResponse;

    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        PacketWriter writer = new();
        writer.Write((uint)0); // Result
        writer.Write((uint)0); // circle_data
        writer.Write((uint)0); // auth_level
        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}