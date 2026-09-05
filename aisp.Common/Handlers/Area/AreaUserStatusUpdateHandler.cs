using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// The status window: a one-line text and an icon choice for the player's own avatar. Stored on the character,
/// carried in the avatar data from then on, acknowledged, and pushed to everyone on the map including the sender.
/// Only the session's own avatar can be updated.
/// </summary>
public sealed class AreaUserStatusUpdateHandler(
    ICharacterRepository characters,
    SharedState state,
    ILogger<AreaUserStatusUpdateHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.UserStatusUpdateRequest;
    public PacketType ResponseType => PacketType.UserStatusUpdateResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = UserStatusUpdateRequest.FromBytes(payload.Span);
        if (
            session.CharacterId == 0
            || request.ObjectId != session.CharacterId
            || !await characters.UpdateUserStatusAsync(
                (int)session.CharacterId,
                request.Status.StatusText,
                request.Status.StatusIconId,
                ct
            )
        )
        {
            logger.LogWarning(
                "UserStatusUpdate from character {CharacterId} for object {ObjectId} refused",
                session.CharacterId,
                request.ObjectId
            );
            await session.SendAsync(
                ResponseType,
                new UserStatusUpdateResponse(1, request.ObjectId).ToBytes(),
                ct
            );
            return;
        }

        if (session.Character is not null)
        {
            session.Character.UserStatusText = request.Status.StatusText;
            session.Character.UserStatusIconId = request.Status.StatusIconId;
        }
        logger.LogInformation(
            "UserStatusUpdate: character {CharacterId} status \"{Text}\" icon {Icon}",
            session.CharacterId,
            request.Status.StatusText,
            request.Status.StatusIconId
        );
        await session.SendAsync(
            ResponseType,
            new UserStatusUpdateResponse(0, request.ObjectId).ToBytes(),
            ct
        );
        var notify = new NotifyUserStatusUpdate(request.ObjectId, request.Status).ToBytes();
        foreach (var other in state.GetAreaPeers(session, includeSelf: true))
            await other.SendAsync(PacketType.NotifyUserStatusUpdate, notify, ct);
    }
}
