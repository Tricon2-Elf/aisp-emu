using aisp.Common.Config;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Options;

namespace aisp.Common.Handlers.Area;

public class AreaAdventureUploadRateGetHandler(IOptions<ServerOptions> serverOptions)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureUploadRateGetRequest;

    public PacketType ResponseType => PacketType.AdventureUploadRateGetResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        // On the original service this was the author's share of each sale in デレ (the in-game currency), in percent; the client displays sale price * rate / 100 as the revenue per copy.
        var rate = (uint)Math.Clamp(serverOptions.Value.AdventureUploadRatePercent, 0, 100);
        var response = new AdventureUploadRateGetResponse(rate);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
