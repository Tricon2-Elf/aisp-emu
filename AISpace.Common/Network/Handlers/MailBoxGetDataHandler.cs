namespace AISpace.Common.Network.Handlers;

public class MailBoxGetDataHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MailBoxGetDataRequest;

    public PacketType ResponseType => PacketType.MailBoxGetDataResponse;

    public MessageDomain Domains => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        using PacketWriter writer = new();
        writer.Write((uint)0); // Result
        writer.Write((uint)0); // mail
        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
