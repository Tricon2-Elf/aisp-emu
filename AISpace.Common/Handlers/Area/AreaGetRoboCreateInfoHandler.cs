using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Handlers.Area;

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
            // Da Capo
            (1, CharadollPersonality.Quiet) => new RoboCreateAppearance(2012020, 10900040),
            (1, CharadollPersonality.Active) => new RoboCreateAppearance(2012030, 10900030),
            // Clannad
            (2, CharadollPersonality.Quiet) => new RoboCreateAppearance(2022030, 10900061),
            (2, CharadollPersonality.Active) => new RoboCreateAppearance(2022020, 10900050),
            // Shuffle!
            (3, CharadollPersonality.Quiet) => new RoboCreateAppearance(2032020, 10900071),
            (3, CharadollPersonality.Active) => new RoboCreateAppearance(2032030, 10900080),
            _ => DefaultAppearance,
        };
    }

    private readonly record struct RoboCreateAppearance(uint ModelId, uint Hairstyle);
}
