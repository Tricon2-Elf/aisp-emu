using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>Accepts removal of the current player's Friend Link placard.</summary>
public sealed class AreaPlacardRemoveHandler
    : PacketHandlerBase<PlacardRemoveRequest, PlacardRemoveResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.PlacardRemoveRequest;
    public override PacketType ResponseType => PacketType.PlacardRemoveResponse;
    public override ServerType ServerType => ServerType.Area;

    public override Task<PlacardRemoveResponse?> HandleAsync(
        PlacardRemoveRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    ) => Task.FromResult<PlacardRemoveResponse?>(new PlacardRemoveResponse(0));
}
