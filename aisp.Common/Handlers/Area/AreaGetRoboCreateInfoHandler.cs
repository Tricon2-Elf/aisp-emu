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

    private static readonly RoboCreateAppearance DefaultAppearance = new(1002011, 10930010);

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
            ? DefaultAppearance
            : ResolveAppearance(character.HomeIslandId, character.CharadollPersonality);
        var response = new GetRoboCreateInfoResponse(
            appearance.ModelId,
            appearance.Hairstyle,
            DefaultEquips
        );
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }

    private static RoboCreateAppearance ResolveAppearance(
        uint homeIslandId,
        CharadollPersonality personality
    )
    {
        if (personality == CharadollPersonality.None)
            personality =
                Random.Shared.Next(2) == 0
                    ? CharadollPersonality.Quiet
                    : CharadollPersonality.Active;

        return (homeIslandId, personality) switch
        {
            // Da Capo — 朝倉由夢 / 白河ななか (hair is baked into the model)
            (1, CharadollPersonality.Quiet) => new RoboCreateAppearance(2012020, 0),
            (1, CharadollPersonality.Active) => new RoboCreateAppearance(2012030, 0),
            // Clannad — 坂上智代 / 藤林杏
            (2, CharadollPersonality.Quiet) => new RoboCreateAppearance(2022030, 0),
            (2, CharadollPersonality.Active) => new RoboCreateAppearance(2022020, 0),
            // Shuffle! — ネリネ / 芙蓉楓
            (3, CharadollPersonality.Quiet) => new RoboCreateAppearance(2032020, 0),
            (3, CharadollPersonality.Active) => new RoboCreateAppearance(2032030, 0),
            _ => DefaultAppearance,
        };
    }

    private readonly record struct RoboCreateAppearance(uint ModelId, uint Hairstyle);
}
