using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Common;

namespace AISpace.Common.Handlers.Common;

public abstract class VersionCheckHandlerBase : IPacketHandler
{
    public PacketType RequestType => PacketType.VersionCheckRequest;
    public PacketType ResponseType => PacketType.VersionCheckResponse;
    public abstract ServerType ServerType { get; }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var req = VersionCheckRequest.FromBytes(payload.Span);
        var resp = new VersionCheckResponse(0, req.Major, req.Minor, req.Version);
        await session.SendAsync(ResponseType, resp.ToBytes(), ct);
    }
}

public class AuthVersionCheckHandler : VersionCheckHandlerBase
{
    public override ServerType ServerType => ServerType.Auth;
}

public class MsgVersionCheckHandler : VersionCheckHandlerBase
{
    public override ServerType ServerType => ServerType.Msg;
}

public class AreaVersionCheckHandler : VersionCheckHandlerBase
{
    public override ServerType ServerType => ServerType.Area;
}
