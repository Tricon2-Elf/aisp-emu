using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaMoneyNpsPointsHandler(IUserRepository userRepo, ILogger<AreaMoneyNpsPointsHandler> logger) : IPacketHandler
{
    private readonly IUserRepository _userRepo = userRepo;
    private readonly ILogger<AreaMoneyNpsPointsHandler> _logger = logger;
    private const ulong NpsPointsLimit = 9999;

    public PacketType RequestType => PacketType.MoneyNpsPointsRequest;
    public PacketType ResponseType => PacketType.MoneyNpsPointsResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        if (!connection.IsAuthenticated || connection.User == null)
        {
            var response = new MoneyNpsPointsResponse(1, 0, NpsPointsLimit);
            await connection.SendAsync(ResponseType, response.ToBytes(), ct);
            return;
        }

        var user = await _userRepo.GetById(connection.User.Id);
        if (user == null)
        {
            var response = new MoneyNpsPointsResponse(1, 0, NpsPointsLimit);
            await connection.SendAsync(ResponseType, response.ToBytes(), ct);
            return;
        }

        var total = (ulong)Math.Max(0, user.NpsPoints);
        var responseOk = new MoneyNpsPointsResponse(0, total, NpsPointsLimit);
        await connection.SendAsync(ResponseType, responseOk.ToBytes(), ct);
    }
}
