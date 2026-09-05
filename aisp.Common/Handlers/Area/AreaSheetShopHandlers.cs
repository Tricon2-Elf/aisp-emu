using aisp.Common.Config;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace aisp.Common.Handlers.Area;

/// <summary>The drama editor's 通販 button: opens the 原稿用紙 shop with the configured unit price.</summary>
public sealed class AreaSheetShopStartHandler(IOptions<ServerOptions> serverOptions)
    : PacketHandlerBase<SheetShopStartRequest, SheetShopStartResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.SheetShopStartRequest;
    public override PacketType ResponseType => PacketType.SheetShopStartResponse;
    public override ServerType ServerType => ServerType.Area;

    public override Task<SheetShopStartResponse?> HandleAsync(
        SheetShopStartRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    ) =>
        Task.FromResult<SheetShopStartResponse?>(
            new SheetShopStartResponse(0, Math.Max(0, serverOptions.Value.AdventureSheetPriceAi))
        );
}

/// <summary>
/// Buying 原稿用紙 for デレ. The request echoes the unit price the window showed; the count is what the player
/// typed. The stock push and the money push go out before the 4-byte result, because the window redraws the
/// remaining-sheet label (and the editor's) from the stored stock in the tick the result lands.
/// </summary>
public sealed class AreaSheetShopBuyHandler(
    IAdventureWorkRepository works,
    IOptions<ServerOptions> serverOptions,
    ILogger<AreaSheetShopBuyHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    /// <summary>The client's own code for a short purse; it renders as its 「デレが足りません」 text.</summary>
    public const uint NotEnoughDereResult = 0xFFFFFF83;
    public const uint RefusedResult = 0xFFFFFFFF;

    public PacketType RequestType => PacketType.SheetShopBuyRequest;
    public PacketType ResponseType => PacketType.SheetShopBuyResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = SheetShopBuyRequest.FromBytes(payload.Span);
        var userId = session.User?.Id ?? session.UserId;
        var unitPrice = Math.Max(0, serverOptions.Value.AdventureSheetPriceAi);
        if (request.SheetCount == 0 || request.SheetPriceAi != unitPrice)
        {
            logger.LogWarning(
                "SheetShopBuy from user {UserId}: {Count} sheets at {Price} refused (unit price is {UnitPrice})",
                userId,
                request.SheetCount,
                request.SheetPriceAi,
                unitPrice
            );
            await session.SendAsync(
                ResponseType,
                new SheetShopBuyResponse(RefusedResult).ToBytes(),
                ct
            );
            return;
        }

        var bought = await works.BuySheetsAsync(
            userId,
            (int)Math.Min(request.SheetCount, AdventureWorkRepository.MaxSheetStock),
            unitPrice,
            ct
        );
        if (bought is null)
        {
            var purse = session.User?.AiPoints ?? 0;
            var result =
                purse < unitPrice * request.SheetCount ? NotEnoughDereResult : RefusedResult;
            logger.LogInformation(
                "SheetShopBuy from user {UserId}: {Count} sheets at {Price} refused",
                userId,
                request.SheetCount,
                unitPrice
            );
            await session.SendAsync(ResponseType, new SheetShopBuyResponse(result).ToBytes(), ct);
            return;
        }

        var (stock, aiPoints) = bought.Value;
        if (session.User is not null)
            session.User.AiPoints = aiPoints;
        logger.LogInformation(
            "SheetShopBuy from user {UserId}: bought {Count} sheets for {Total} デレ; stock {Stock}, {AiPoints} left",
            userId,
            request.SheetCount,
            unitPrice * request.SheetCount,
            stock,
            aiPoints
        );
        await session.SendAsync(
            PacketType.AdventureUpdatedSheetStackNotify,
            new AdventureUpdatedSheetStackNotify((uint)stock).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.MoneyUpdatedAipoint,
            new MoneyUpdatedAipointNotify((ulong)Math.Max(0, aiPoints)).ToBytes(),
            ct
        );
        await session.SendAsync(ResponseType, new SheetShopBuyResponse(0).ToBytes(), ct);
    }
}

/// <summary>The sheet shop window's close button; result 0 closes it and the editor resumes.</summary>
public sealed class AreaSheetShopEndHandler
    : PacketHandlerBase<SheetShopEndRequest, SheetShopEndResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.SheetShopEndRequest;
    public override PacketType ResponseType => PacketType.SheetShopEndResponse;
    public override ServerType ServerType => ServerType.Area;

    public override Task<SheetShopEndResponse?> HandleAsync(
        SheetShopEndRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    ) => Task.FromResult<SheetShopEndResponse?>(new SheetShopEndResponse(0));
}
