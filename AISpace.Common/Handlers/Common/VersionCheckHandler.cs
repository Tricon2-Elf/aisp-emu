using AISpace.Common.Network.Packets.Common;
using AISpace.Network;
using AISpace.Network.Packets.Common;

namespace AISpace.Common.Handlers.Common;

public abstract class VersionCheckHandlerBase : IPacketHandler
{
    public PacketType RequestType => PacketType.VersionCheckRequest;
    public PacketType ResponseType => PacketType.VersionCheckResponse;
    public abstract MessageDomain Domain { get; }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var req = VersionCheckRequest.FromBytes(payload.Span);
        var resp = new VersionCheckResponse(0, req.Major, req.Minor, req.Version);
        await connection.SendAsync(ResponseType, resp.ToBytes(), ct);
    }
}

public class AuthVersionCheckHandler : VersionCheckHandlerBase
{
    public override MessageDomain Domain => MessageDomain.Auth;
}

public class MsgVersionCheckHandler : VersionCheckHandlerBase
{
    public override MessageDomain Domain => MessageDomain.Msg;
}

public class AreaVersionCheckHandler : VersionCheckHandlerBase
{
    public override MessageDomain Domain => MessageDomain.Area;
}
