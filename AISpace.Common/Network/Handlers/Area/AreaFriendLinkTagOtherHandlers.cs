namespace AISpace.Common.Network.Handlers;

public class AreaFriendLinkTagOtherHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.FriendLinkTagGetOtherRequest;
    public PacketType ResponseType => (PacketType)0x239E; // FriendLinkTagGetResponse
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var reader = new PacketReader(payload.Span);
        uint targetObjId = reader.ReadUInt();

        var writer = new PacketWriter();
        writer.Write((uint)0); // Result
        writer.Write(targetObjId);
        writer.Write((uint)0); // tagdata
        writer.Write((uint)0); // slot
        writer.Write((uint)0); // questionnaire_tagdata
        writer.Write((uint)0); // questionnaire_slot

        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
