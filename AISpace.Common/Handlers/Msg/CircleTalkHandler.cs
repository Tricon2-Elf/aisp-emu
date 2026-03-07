using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Handlers.Msg;

public class CircleTalkHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.CircleTalkRequest;
    public PacketType ResponseType => (PacketType)0xA9C1;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        // In the circle chat, the client sends MessageID (4) + BalloonID (4) + Text...
        // To avoid crashing, you need to return THE SAME MessageID
        var reader = new PacketReader(payload.Span);
        uint msgId = reader.ReadUInt();

        var writer = new PacketWriter();
        writer.Write(msgId); // Message ID from the request
        writer.Write((uint)0); // Result = Success

        await session.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
