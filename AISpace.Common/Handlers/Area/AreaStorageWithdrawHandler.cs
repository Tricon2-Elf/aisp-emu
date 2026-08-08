using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

/// <summary>
/// send_storage_withdraw (0x9C26) → recv_storage_withdraw_r (0xE42A).
/// Moves AI points from wardrobe deposit into the purse.
/// </summary>
public sealed class AreaStorageWithdrawHandler(
    IUserRepository userRepo,
    ILogger<AreaStorageWithdrawHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.StorageWithdrawRequest;
    public PacketType ResponseType => PacketType.StorageWithdrawResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.User is null)
        {
            await session.SendAsync(ResponseType, new StorageWithdrawResponse(1, 0).ToBytes(), ct);
            return;
        }

        StorageWithdrawRequest request;
        try
        {
            request = StorageWithdrawRequest.FromBytes(payload.Span);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to parse StorageWithdrawRequest for user {UserId}",
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
        var user = await userRepo.TransferStorageDepositAsync(session.User.Id, -amount, ct);
        if (user is null)
        {
            await FailAsync(session, ct);
            return;
        }

        session.User.AiPoints = user.AiPoints;
        session.User.StorageDeposit = user.StorageDeposit;

        var deposit = (ulong)Math.Max(0, user.StorageDeposit);
        await session.SendAsync(
            ResponseType,
            new StorageWithdrawResponse(0, deposit).ToBytes(),
            ct
        );
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
        await session.SendAsync(
            ResponseType,
            new StorageWithdrawResponse(1, deposit).ToBytes(),
            ct
        );
    }
}
