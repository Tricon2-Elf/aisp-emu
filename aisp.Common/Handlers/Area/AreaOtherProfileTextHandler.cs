using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaOtherProfileTextHandler(SharedState state)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.OtherProfileTextRequest;
    public PacketType ResponseType => PacketType.GetMyAvatarMyprofileDataResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = OtherProfileTextRequest.FromBytes(payload.Span);
        var target = state.GetAreaSessionByCharacterId(
            req.TargetObjectId,
            session.MapId,
            session.ChannelId
        );
        var cha = target?.Character ?? target?.User?.Characters.FirstOrDefault();

        var profile =
            cha != null
                ? new ProfileData(
                    cha.Like1 ?? "None",
                    cha.Like2 ?? "None",
                    cha.Like3 ?? "None",
                    cha.LikeDesc1 ?? "",
                    cha.LikeDesc2 ?? "",
                    cha.LikeDesc3 ?? "",
                    cha.AvatarDesc ?? "Hello!"
                )
                : new ProfileData("", "", "", "", "", "", "");

        var response = new GetMyAvatarMyprofileDataResponse(profile);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
