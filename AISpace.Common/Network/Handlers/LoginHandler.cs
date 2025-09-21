using System.Text;
using AISpace.Common.Network.Packets.Msg;
using NLog;

namespace AISpace.Common.Network.Handlers;

public class LoginHandler : IPacketHandler
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public PacketType RequestType => PacketType.LoginRequest;

    public PacketType ResponseType => PacketType.LoginResponse;

    public MessageDomain Domains => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var loginReq = LoginRequest.FromBytes(payload.Span);
        var otp = Encoding.ASCII.GetString(loginReq.OTP);
        _logger.Info($"Client: {connection.Id} LoginRequest UserID: {loginReq.UserID}, OTP: {otp}");
        await connection.SendAsync(ResponseType, new LoginResponse().ToBytes(), ct);
    }
}
