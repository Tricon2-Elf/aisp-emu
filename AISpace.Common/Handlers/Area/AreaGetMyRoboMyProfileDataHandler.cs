using AISpace.Common.DAL;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaGetMyRoboMyProfileDataHandler(MainContext db)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    private static readonly ProfileData EmptyProfile = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty
    );

    public PacketType RequestType => PacketType.GetMyRoboMyProfileDataRequest;
    public PacketType ResponseType => PacketType.GetMyRoboMyProfileDataResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GetMyRoboMyProfileDataRequest.FromBytes(payload.Span);
        var robo = await db
            .Robos.AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.CharacterId == checked((int)session.CharacterId)
                    && x.RoboId == request.RoboId,
                ct
            );

        if (robo is null)
        {
            await session.SendAsync(
                ResponseType,
                new GetMyRoboMyProfileDataResponse(1, EmptyProfile).ToBytes(),
                ct
            );
            return;
        }

        var profile = new ProfileData(
            robo.Like1,
            robo.Like2,
            robo.Like3,
            robo.LikeDesc1,
            robo.LikeDesc2,
            robo.LikeDesc3,
            robo.ProfileDescription
        );
        var metadata = new AvatarProfileMetadata(
            CalculatePlayDurationDays(robo.CreatedAt),
            robo.ProfileUnknownDword04,
            robo.ProfileUnknownDword08
        );
        await session.SendAsync(
            ResponseType,
            new GetMyRoboMyProfileDataResponse(0, profile, metadata).ToBytes(),
            ct
        );
    }

    private static uint CalculatePlayDurationDays(DateTime createdAt)
    {
        var utcCreatedAt = createdAt.Kind switch
        {
            DateTimeKind.Utc => createdAt,
            DateTimeKind.Local => createdAt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
        };
        var elapsed = DateTime.UtcNow - utcCreatedAt;
        if (elapsed <= TimeSpan.Zero)
            return 0;

        return (uint)Math.Min(Math.Floor(elapsed.TotalDays), uint.MaxValue);
    }
}
