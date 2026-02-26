using AISpace.Common.Network.Packets.Area;
using AISpace.Common.DAL;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Network.Handlers;

public class AreaGetMyAvatarMyprofileDataHandler(MainContext db) : IPacketHandler
{
    public PacketType RequestType => PacketType.GetMyAvatarMyprofileDataRequest;
    public PacketType ResponseType => PacketType.GetMyAvatarMyprofileDataResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var cha = await db.Characters.FirstOrDefaultAsync(c => c.Id == connection.CharacterId, ct);
        if (cha != null)
        {
            var response = new GetMyAvatarMyprofileDataResponse(cha);
            await connection.SendAsync(ResponseType, response.ToBytes(), ct);
        }
    }
}