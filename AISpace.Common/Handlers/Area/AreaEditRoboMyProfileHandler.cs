using AISpace.Common.DAL;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaEditRoboMyProfileHandler(MainContext db)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EditRoboMyProfileRequest;
    public PacketType ResponseType => PacketType.EditRoboMyProfileResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = EditRoboMyProfileRequest.FromBytes(payload.Span);
        var robo = await db.Robos.SingleOrDefaultAsync(
            x => x.CharacterId == checked((int)session.CharacterId) && x.RoboId == request.RoboId,
            ct
        );

        if (robo is null)
        {
            await session.SendAsync(ResponseType, new EditRoboMyProfileResponse(1).ToBytes(), ct);
            return;
        }

        robo.Like1 = request.Profile.Like1;
        robo.Like2 = request.Profile.Like2;
        robo.Like3 = request.Profile.Like3;
        robo.LikeDesc1 = request.Profile.LikeDesc1;
        robo.LikeDesc2 = request.Profile.LikeDesc2;
        robo.LikeDesc3 = request.Profile.LikeDesc3;
        robo.ProfileDescription = request.Profile.AvatarDesc;
        // The client echoes the metadata and current job unchanged; neither is editable in this UI.
        // Keep the server-owned values instead of trusting the echoed request fields.
        robo.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await session.SendAsync(ResponseType, new EditRoboMyProfileResponse(0).ToBytes(), ct);
    }
}
