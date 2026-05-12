using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Handlers.Msg;

public class MailBoxGetDataHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MailBoxGetDataRequest;

    public PacketType ResponseType => PacketType.MailBoxGetDataResponse;

    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        PacketWriter writer = new();
        writer.Write((uint)0); // Result
        writer.Write((uint)0); // mail
        await session.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
