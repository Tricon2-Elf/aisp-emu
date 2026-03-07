using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMoneyNpsPointsHandler(IUserRepository userRepo, ILogger<AreaMoneyNpsPointsHandler> logger) : IPacketHandler
{
    private readonly IUserRepository _userRepo = userRepo;
    private readonly ILogger<AreaMoneyNpsPointsHandler> _logger = logger;
    private const ulong NpsPointsLimit = 9999;

    public PacketType RequestType => PacketType.MoneyNpsPointsRequest;
    public PacketType ResponseType => PacketType.MoneyNpsPointsResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (!session.IsAuthenticated || session.User == null)
        {
            var response = new MoneyNpsPointsResponse(1, 0, NpsPointsLimit);
            await session.SendAsync(ResponseType, response.ToBytes(), ct);
            return;
        }

        var user = await _userRepo.GetById(session.User.Id);
        if (user == null)
        {
            var response = new MoneyNpsPointsResponse(1, 0, NpsPointsLimit);
            await session.SendAsync(ResponseType, response.ToBytes(), ct);
            return;
        }

        var total = (ulong)Math.Max(0, user.NpsPoints);
        var responseOk = new MoneyNpsPointsResponse(0, total, NpsPointsLimit);
        await session.SendAsync(ResponseType, responseOk.ToBytes(), ct);
    }
}
