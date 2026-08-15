using aisp.Common.DAL;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Handlers.Area;

public class AreaMyProfileAvatarEditHandler(MainContext db)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyProfileAvatarEditRequest;
    public PacketType ResponseType => PacketType.MyProfileAvatarEditResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = MyProfileAvatarEditRequest.FromBytes(payload.Span);

        var cha = await db.Characters.FirstOrDefaultAsync(c => c.Id == session.CharacterId, ct);
        if (cha != null)
        {
            cha.Like1 = req.Like1;
            cha.Like2 = req.Like2;
            cha.Like3 = req.Like3;
            cha.LikeDesc1 = req.LikeDesc1;
            cha.LikeDesc2 = req.LikeDesc2;
            cha.LikeDesc3 = req.LikeDesc3;
            cha.AvatarDesc = req.AvatarDesc;
            await db.SaveChangesAsync(ct);
        }

        await session.SendAsync(ResponseType, new MyProfileAvatarEditResponse(0).ToBytes(), ct);
    }
}
