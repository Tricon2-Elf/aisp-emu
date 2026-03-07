using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Msg;

public class MailBoxGetDataHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MailBoxGetDataRequest;

    public PacketType ResponseType => PacketType.MailBoxGetDataResponse;

    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        PacketWriter writer = new();
        writer.Write((uint)0); // Result
        writer.Write((uint)0); // mail
        await session.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
