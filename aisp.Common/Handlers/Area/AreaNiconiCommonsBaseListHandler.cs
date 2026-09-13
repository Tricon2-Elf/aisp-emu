using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNiconiCommonsBaseListHandler(
    ICharacterRepository characters,
    DramaCatalog catalog
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NiconiCommonsBaseListRequest;
    public PacketType ResponseType => PacketType.NiconiCommonsBaseListResponse;
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
        var rows = await catalog.CommonsAsync(character, session, ct);
        var response = new NiconiCommonsBaseListResponse(0, rows);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
