namespace AISpace.Common.Network.Handlers;

public class AreaFriendLinkTagGetHandler : IPacketHandler
{
    public PacketType RequestType => (PacketType)0x0F97; // Тот самый 3991
    public PacketType ResponseType => (PacketType)0x239E; // recv_get_friend_link_tag_r
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

        // Total 24 bytes (6 fields by 4 bytes)
        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
