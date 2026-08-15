using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Game;

internal static class StorageSession
{
    public static async Task OpenAsync(
        IPlayerSession session,
        StorageOpenContext context,
        CancellationToken ct = default
    )
    {
        session.StorageOpenContext = context;

        // Client sets a "opened via furniture/myroom" flag from storage_furn_open_r
        // before recv_storage_opened actually shows PAS 1120.
        await session.SendAsync(
            PacketType.StorageFurnOpenResponse,
            new StorageFurnOpenResponse(0).ToBytes(),
            ct
        );

        var deposit = (ulong)Math.Max(0, session.User?.StorageDeposit ?? 0);
        var aiPoints = (ulong)Math.Max(0, session.User?.AiPoints ?? 0);
        await session.SendAsync(
            PacketType.MoneyUpdatedAipoint,
            new MoneyUpdatedAipointNotify(aiPoints).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.StorageOpenedNotify,
            new StorageOpenedNotify(deposit).ToBytes(),
            ct
        );
    }

    public static async Task CloseAsync(IPlayerSession session, CancellationToken ct = default)
    {
        await session.SendAsync(
            PacketType.StorageCloseResponse,
            new StorageCloseResponse(0).ToBytes(),
            ct
        );

        if (session.StorageOpenContext == StorageOpenContext.None)
            return;

        await session.SendAsync(
            PacketType.StorageFurnCloseResponse,
            new StorageFurnCloseResponse(0).ToBytes(),
            ct
        );
        session.StorageOpenContext = StorageOpenContext.None;
    }
}
