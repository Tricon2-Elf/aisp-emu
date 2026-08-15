using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaMyRoomUpdateSecurityHandler(
    IMyRoomRepository myRoomRepository,
    SharedState state,
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ILogger<AreaMyRoomUpdateSecurityHandler> logger
)
    : PacketHandlerBase<MyRoomUpdateSecurityRequest, MyRoomUpdateSecurityResponse>,
        IRequiresAuthenticatedSession
{
    private static readonly IReadOnlyDictionary<uint, uint> ShoppingAreaMapIds = new Dictionary<
        uint,
        uint
    >
    {
        [1] = 10_010_200,
        [2] = 10_020_200,
        [3] = 10_030_200,
    };

    public override PacketType RequestType => PacketType.MyRoomUpdateSecurityRequest;
    public override PacketType ResponseType => PacketType.MyRoomUpdateSecurityResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<MyRoomUpdateSecurityResponse?> HandleAsync(
        MyRoomUpdateSecurityRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            !await MyRoomRequestValidation.IsOwnerInRoomAsync(
                request.RoomId,
                session,
                myRoomRepository,
                ct
            )
        )
            return new MyRoomUpdateSecurityResponse(1);

        var updated = await myRoomRepository.UpdateSecurityAsync(
            checked((int)request.RoomId),
            checked((int)session.CharacterId),
            request.Security,
            ct
        );
        if (!updated)
            return new MyRoomUpdateSecurityResponse(1);

        var securityNotify = new NotifyMyHouseChangeSecurity(
            request.RoomId,
            request.Security
        ).ToBytes();
        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            request.RoomId,
            PacketType.NotifyMyHouseChangeSecurity,
            securityNotify,
            includeSource: true,
            ct
        );

        if (request.Security != MyRoomSecurity.Public)
            await EjectGuestsAsync(session, request.RoomId, ct);

        return new MyRoomUpdateSecurityResponse(0);
    }

    private async Task EjectGuestsAsync(IPlayerSession owner, uint roomId, CancellationToken ct)
    {
        var guests = state
            .GetAreaPeers(owner, includeSelf: false)
            .Where(peer => peer.MyRoomId == roomId && peer.CharacterId != owner.CharacterId)
            .ToList();

        foreach (var guest in guests)
        {
            await guest.SendAsync(
                PacketType.MyRoomThrowoutOthersResponse,
                new MyRoomThrowoutOthersResponse(0).ToBytes(),
                ct
            );

            var character = await directMapLinkTransitionService.ResolveCharacterAsync(guest, ct);
            uint destinationMapId = MyRoomDoorServerScript.AkihabaraUdxMapId;
            if (
                character is not null
                && ShoppingAreaMapIds.TryGetValue(character.HomeIslandId, out var shoppingMapId)
            )
                destinationMapId = shoppingMapId;

            if (
                !await directMapLinkTransitionService.TryTeleportToMapAsync(
                    guest,
                    destinationMapId,
                    ct
                )
            )
            {
                logger.LogWarning(
                    "Failed to teleport ejected guest {GuestCharacterId} from room {RoomId} to map {MapId}",
                    guest.CharacterId,
                    roomId,
                    destinationMapId
                );
            }
        }
    }
}
