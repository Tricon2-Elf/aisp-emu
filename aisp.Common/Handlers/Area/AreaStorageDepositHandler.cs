using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// send_storage_deposit (0x51A4) → recv_storage_deposit_r (0x541C).
/// Moves AI points from the purse into wardrobe deposit.
/// </summary>
public sealed class AreaStorageDepositHandler(
    IUserRepository userRepo,
    ILogger<AreaStorageDepositHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.StorageDepositRequest;
    public PacketType ResponseType => PacketType.StorageDepositResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.User is null)
        {
            await session.SendAsync(ResponseType, new StorageDepositResponse(1, 0).ToBytes(), ct);
            return;
        }

        StorageDepositRequest request;
        try
        {
            request = StorageDepositRequest.FromBytes(payload.Span);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to parse StorageDepositRequest for user {UserId}",
                session.User.Id
            );
            await FailAsync(session, ct);
            return;
        }

        if (request.AiPoint == 0 || request.AiPoint > long.MaxValue)
        {
            await FailAsync(session, ct);
            return;
        }

        var amount = (long)request.AiPoint;
        var user = await userRepo.TransferStorageDepositAsync(session.User.Id, amount, ct);
        if (user is null)
        {
            await FailAsync(session, ct);
            return;
        }

        session.User.AiPoints = user.AiPoints;
        session.User.StorageDeposit = user.StorageDeposit;

        var deposit = (ulong)Math.Max(0, user.StorageDeposit);
        await session.SendAsync(ResponseType, new StorageDepositResponse(0, deposit).ToBytes(), ct);
        await session.SendAsync(
            PacketType.MoneyUpdatedAipoint,
            new MoneyUpdatedAipointNotify((ulong)Math.Max(0, user.AiPoints)).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.StorageUpdatedDepositNotify,
            new StorageUpdatedDepositNotify(deposit).ToBytes(),
            ct
        );
    }

    private async Task FailAsync(IPlayerSession session, CancellationToken ct)
    {
        var deposit = (ulong)Math.Max(0, session.User?.StorageDeposit ?? 0);
        await session.SendAsync(ResponseType, new StorageDepositResponse(1, deposit).ToBytes(), ct);
    }
}
