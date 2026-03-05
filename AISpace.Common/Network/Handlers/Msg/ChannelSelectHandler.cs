using AISpace.Common.DAL;
using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class ChannelSelectHandler(ILogger<ChannelSelectHandler> logger, IServiceScopeFactory scopeFactory) : IPacketHandler
{
    public PacketType RequestType => PacketType.ChannelSelectRequest;
    public PacketType ResponseType => PacketType.ChannelSelectResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = ChannelSelectRequest.FromBytes(payload.Span);
        logger.LogInformation("ChannelSelectRequest from user {UserId}: ChannelID {ChannelId}", connection.User?.Id ?? 0, request.ChannelID);
        string myIp = "aisp.moe";
        ushort areaPort = 50054;
        uint mapID = 10990100;
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainContext>();
            if (connection.User != null)
            {
                var character = await db.Characters.FirstOrDefaultAsync(c => c.UserId == connection.User.Id, ct);
                if (character != null)
                {
                    character.CurrentMapId = mapID;
                    await db.SaveChangesAsync(ct);
                }
            }
        }

        var serverInfo = new ServerInfo(myIp, areaPort);
        var response = new ChannelSelectResponse(0, serverInfo, mapID, mapID);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
