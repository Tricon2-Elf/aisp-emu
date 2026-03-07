using AISpace.Common.Network.Packets.Msg;
using AISpace.Network;

namespace AISpace.Common.Handlers.Msg;

public class AvatarSelectHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarSelectRequest;
    public PacketType ResponseType => PacketType.AvatarSelectResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        if (connection.User == null)
            return;
        var cha = connection.User.Characters.FirstOrDefault();

        var response = new AvatarSelectResponse(0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
