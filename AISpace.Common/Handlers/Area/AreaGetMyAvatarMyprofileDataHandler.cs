using AISpace.Common.DAL;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Handlers.Area;

public class AreaGetMyAvatarMyprofileDataHandler(MainContext db)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GetMyAvatarMyprofileDataRequest;
    public PacketType ResponseType => PacketType.GetMyAvatarMyprofileDataResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var cha = await db.Characters.FirstOrDefaultAsync(c => c.Id == session.CharacterId, ct);
        if (cha != null)
        {
            var profileData = new ProfileData(
                cha.Like1,
                cha.Like2,
                cha.Like3,
                cha.LikeDesc1,
                cha.LikeDesc2,
                cha.LikeDesc3,
                cha.AvatarDesc
            );
            var response = new GetMyAvatarMyprofileDataResponse(profileData);
            await session.SendAsync(ResponseType, response.ToBytes(), ct);
        }
    }
}
