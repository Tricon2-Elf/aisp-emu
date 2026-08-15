using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaMoneyNpsPointsHandler(
    IUserRepository userRepo,
    ILogger<AreaMoneyNpsPointsHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    private readonly IUserRepository _userRepo = userRepo;
    private readonly ILogger<AreaMoneyNpsPointsHandler> _logger = logger;
    private const ulong NpsPointsLimit = 9999;

    public PacketType RequestType => PacketType.MoneyNpsPointsRequest;
    public PacketType ResponseType => PacketType.MoneyNpsPointsResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetById(session.User!.Id);
        if (user == null)
        {
            var response = new MoneyNpsPointsResponse(1, 0, NpsPointsLimit);
            await session.SendAsync(ResponseType, response.ToBytes(), ct);
            return;
        }

        var total = (ulong)Math.Max(0, user.AiPoints);
        var responseOk = new MoneyNpsPointsResponse(0, total, NpsPointsLimit);
        await session.SendAsync(ResponseType, responseOk.ToBytes(), ct);

        var aiPoints = (ulong)Math.Max(0, user.AiPoints);
        await session.SendAsync(
            PacketType.MoneyUpdatedAipoint,
            new MoneyUpdatedAipointNotify(aiPoints).ToBytes(),
            ct
        );

        var niconicoPoints = (ulong)Math.Max(0, user.NicoPoints);
        await session.SendAsync(
            PacketType.MoneyUpdatedNicopoint,
            new MoneyUpdatedNicopointNotify(niconicoPoints).ToBytes(),
            ct
        );
    }
}
