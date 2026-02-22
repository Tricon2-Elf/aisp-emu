using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network.Crypto;
using AISpace.Common.Network.Packets.Auth;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Auth;

public class WorldSelectHandler(IWorldRepository worldRepo, IUserSessionRepository sessionRepo, ILogger<WorldSelectHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.Auth_WorldSelectRequest;
    public PacketType ResponseType => PacketType.Auth_WorldSelectResponse;
    public MessageDomain Domain => MessageDomain.Auth;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var req = WorldSelectRequest.FromBytes(payload.Span);
        var world = await worldRepo.GetByIdAsync((int)req.WorldID);
        
        if (world == null || connection.User == null) return;

        User clientUser = connection.User!;
        string otp = CryptoUtils.GenerateOTP();
        
        await sessionRepo.CreateAsync(clientUser.Id, otp, TimeSpan.FromHours(1), ct);

        string myPublicIp = "192.168.31.158"; 
        ushort msgPort = 50052;

        logger.LogInformation($"[HARDCODE] Sending client {clientUser.Username} to {myPublicIp}:{msgPort}");

        var WorldSelectResp = new WorldSelectResponse(0, myPublicIp, msgPort, otp);
        await connection.SendAsync(PacketType.Auth_WorldSelectResponse, WorldSelectResp.ToBytes(), ct);
    }
}