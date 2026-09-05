using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Handlers.Area;

public sealed class AreaGetRoboCreateInfoHandler(MainContext db)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    private static readonly ItemSlotInfo[] DefaultEquips =
    [
        new(10100060, 0), // Shirt
        new(10200090, 0), // Shorts
        new(10400000, 0), // Socks
        new(10500010, 0), // Shoes
    ];

    public PacketType RequestType => PacketType.GetRoboCreateInfoRequest;
    public PacketType ResponseType => PacketType.GetRoboCreateInfoResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        _ = GetRoboCreateInfoRequest.FromBytes(payload.Span);
        var character = await db
            .Characters.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == checked((int)session.CharacterId), ct);
        var appearance = character is null
            ? CharadollAppearance.Default
            : ResolveCreateAppearance(character.HomeIslandId, character.CharadollPersonality);
        var response = new GetRoboCreateInfoResponse(
            appearance.ModelId,
            appearance.Hairstyle,
            DefaultEquips
        );
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }

    private static CharadollAppearance.Appearance ResolveCreateAppearance(
        uint homeIslandId,
        CharadollPersonality personality
    )
    {
        if (personality == CharadollPersonality.None)
            personality =
                Random.Shared.Next(2) == 0
                    ? CharadollPersonality.Quiet
                    : CharadollPersonality.Active;

        return CharadollAppearance.Resolve(homeIslandId, personality);
    }
}
