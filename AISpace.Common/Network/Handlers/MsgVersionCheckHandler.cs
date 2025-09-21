using AISpace.Common.Network.Packets.Common;

namespace AISpace.Common.Network.Handlers;

public class MsgVersionCheckHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.Msg_VersionCheckRequest;

    public PacketType ResponseType => PacketType.Msg_VersionCheckResponse;

    public MessageDomain Domains => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var req = VersionCheckRequest.FromBytes(payload.Span);
        var resp = new VersionCheckResponse(0, req.Major, req.Minor, req.Version);
        await connection.SendAsync(ResponseType, resp.ToBytes(), ct);
    }
}
