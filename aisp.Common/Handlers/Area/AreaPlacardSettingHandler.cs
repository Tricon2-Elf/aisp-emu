using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Creates a client-visible Friend Link placard response.
/// </summary>
public sealed class AreaPlacardSettingHandler(IFriendRepository friends)
    : PacketHandlerBase<PlacardSettingRequest, PlacardSettingResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.PlacardSettingRequest;
    public override PacketType ResponseType => PacketType.PlacardSettingResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<PlacardSettingResponse?> HandleAsync(
        PlacardSettingRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var placardId = session.CharacterId;
        var name = session.Character?.Name ?? string.Empty;
        var tagId = request.Slot + 1;
        var tags = await friends.GetLinkTagsAsync((int)session.CharacterId, ct);
        var tagName = tags.FirstOrDefault(x => x.Slot == request.Slot)?.Name ?? string.Empty;
        await session.SendAsync(
            PacketType.NotifyPlacardSetting,
            new NotifyPlacardSetting(
                placardId,
                name,
                session.CharacterId,
                tagId,
                request.Slot,
                request.Direction,
                tagName,
                request.Position
            ).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.NotifyPlacardInMap,
            new NotifyPlacardInMap(
                placardId,
                name,
                session.CharacterId,
                tagId,
                request.Slot,
                request.Direction,
                tagName,
                request.Position
            ).ToBytes(),
            ct
        );
        return new PlacardSettingResponse(
            result: 0,
            placardId: placardId,
            ownerName: name,
            ownerAvatarId: session.CharacterId,
            tagId: tagId,
            slot: request.Slot,
            direction: request.Direction,
            tagName: tagName,
            position: request.Position
        );
    }
}
