using aisp.Common.DAL;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Msg;

public class AvatarDestroyHandler(MainContext db, ILogger<AvatarDestroyHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AvatarDestroyRequest;
    public PacketType ResponseType => PacketType.AvatarDestroyResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        // Find the first character (as the emulator currently supports only one)
        var cha = session.User!.Characters.FirstOrDefault();

        if (cha != null)
        {
            logger.LogInformation(
                "[DELETE] Removing character '{CharacterName}' ({CharacterId}) for user {Username}",
                cha.Name,
                cha.Id,
                session.User.Username
            );

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var persistedCharacter = await db.Characters.SingleOrDefaultAsync(
                character => character.Id == cha.Id && character.UserId == session.User.Id,
                ct
            );
            if (persistedCharacter is null)
            {
                await transaction.RollbackAsync(ct);
                await session.SendAsync(ResponseType, new AvatarDestroyResponse(0).ToBytes(), ct);
                return;
            }

            // These relationships intentionally use RESTRICT. A deleted leader dissolves their
            // circles; requests involving the deleted character are no longer actionable.
            await db
                .CircleJoinRequests.Where(request =>
                    request.RequesterCharacterId == cha.Id || request.TargetCharacterId == cha.Id
                )
                .ExecuteDeleteAsync(ct);
            await db
                .FriendRequests.Where(request =>
                    request.RequesterCharacterId == cha.Id || request.TargetCharacterId == cha.Id
                )
                .ExecuteDeleteAsync(ct);
            await db
                .Friendships.Where(friendship =>
                    friendship.CharacterIdLow == cha.Id || friendship.CharacterIdHigh == cha.Id
                )
                .ExecuteDeleteAsync(ct);

            var ledCircleIds = await db
                .Circles.Where(circle => circle.LeaderCharacterId == cha.Id)
                .Select(circle => circle.Id)
                .ToListAsync(ct);
            if (ledCircleIds.Count > 0)
            {
                // Character.CircleId is the legacy single-circle link and its database FK uses
                // NO ACTION, so every reference must be cleared before dissolving a led circle.
                await db
                    .Characters.Where(character =>
                        character.CircleId.HasValue
                        && ledCircleIds.Contains(character.CircleId.Value)
                    )
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(character => character.CircleId, (int?)null),
                        ct
                    );
            }

            await db
                .Circles.Where(circle => circle.LeaderCharacterId == cha.Id)
                .ExecuteDeleteAsync(ct);

            db.Characters.Remove(persistedCharacter);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            session.User.Characters.Remove(cha);
            session.Character = null;
            session.CharacterId = 0;
        }

        var response = new AvatarDestroyResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
