using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaFriendLinkTagOtherHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.FriendLinkTagGetOtherRequest;
    public PacketType ResponseType => (PacketType)0x239E; // FriendLinkTagGetResponse
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
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

        await session.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
