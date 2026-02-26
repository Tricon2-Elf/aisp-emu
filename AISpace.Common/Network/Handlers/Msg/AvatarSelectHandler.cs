using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers.Msg;

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

        if (cha != null)
        {
            connection.CharacterId = (uint)cha.Id;
        }

        var response = new AvatarSelectResponse(0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
