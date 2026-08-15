using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaAvatarProfileGetDataHandler(SharedState state)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AvatarProfileGetDataRequest;
    public PacketType ResponseType => PacketType.AvatarProfileGetDataResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = AvatarProfileGetDataRequest.FromBytes(payload.Span);
        var target = state.GetAreaSessionByCharacterId(
            req.TargetObjectId,
            session.MapId,
            session.ChannelId
        );
        var cha = target?.Character ?? target?.User?.Characters.FirstOrDefault();

        ProfileData? profile =
            cha != null
                ? new ProfileData(
                    cha.Like1 ?? "",
                    cha.Like2 ?? "",
                    cha.Like3 ?? "",
                    cha.LikeDesc1 ?? "",
                    cha.LikeDesc2 ?? "",
                    cha.LikeDesc3 ?? "",
                    cha.AvatarDesc ?? ""
                )
                : null;

        var response = new AvatarProfileGetDataResponse(0, req.TargetObjectId, profile);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
