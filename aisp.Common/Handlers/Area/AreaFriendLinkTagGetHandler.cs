using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaFriendLinkTagGetHandler(IFriendRepository friends)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.FriendLinkTagGetRequest;
    public PacketType ResponseType => PacketType.FriendLinkTagGetResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = FriendLinkTagGetRequest.FromBytes(payload.Span);
        if (session.CharacterId > int.MaxValue)
        {
            await session.SendAsync(
                ResponseType,
                new FriendLinkTagGetResponse(1, req.TargetObjectId).ToBytes(),
                ct
            );
            return;
        }

        var savedTags = await friends.GetLinkTagsAsync((int)session.CharacterId, ct);
        var populatedTags = savedTags
            .Where(tag => tag.Slot <= 4 && !string.IsNullOrWhiteSpace(tag.Name))
            .ToArray();
        var arbitraryTags = populatedTags
            .Select(tag => new FriendLinkTagData(tag.Slot + 1, tag.Name))
            .ToArray();
        var arbitrarySlots = populatedTags.Select(tag => tag.Slot).ToArray();

        var response = new FriendLinkTagGetResponse(
            0,
            req.TargetObjectId,
            arbitraryTags,
            arbitrarySlots
        );
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
