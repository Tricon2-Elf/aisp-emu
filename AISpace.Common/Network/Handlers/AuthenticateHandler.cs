using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network.Packets.Auth;
using NLog;

namespace AISpace.Common.Network.Handlers;

internal class AuthenticateHandler(IUserRepository repo) : IPacketHandler
{
    public PacketType RequestType => PacketType.AuthenticateRequest;
    public PacketType ResponseType => PacketType.AuthenticateResponse;

    public MessageDomain Domains => MessageDomain.Auth;

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var req = AuthenticateRequest.FromBytes(payload.Span);
        _logger.Info($"Username: '{req.Username}', Password: {req.Password}");
        //TODO: Implement a check to repo
        bool valid = await repo.ValidateCredentialsAsync(req.Username, req.Password);
        uint userID = 31874;
        var AuthResp = new AuthenticateResponse(userID);
        await connection.SendAsync(ResponseType, AuthResp.ToBytes(), ct);
    }
}
