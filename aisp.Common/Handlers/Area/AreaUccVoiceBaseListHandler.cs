using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaUccVoiceBaseListHandler(
    ICharacterRepository characters,
    DramaCatalog catalog
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.UccVoiceBaseListRequest;
    public PacketType ResponseType => PacketType.UccVoiceBaseListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var character =
            session.CharacterId == 0
                ? null
                : await characters.GetByIdAsync(checked((int)session.CharacterId), ct);
        var rows = await catalog.VoicesAsync(character, session, ct);
        var response = new UccVoiceBaseListResponse(rows);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
