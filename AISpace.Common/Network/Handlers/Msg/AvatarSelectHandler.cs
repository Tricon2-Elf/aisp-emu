using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers.Msg;

public class AvatarSelectHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarSelectRequest;
    public PacketType ResponseType => PacketType.AvatarSelectResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // Как только игрок нажал "Играть", он получает CharacterId
        if (connection.User != null && connection.User.Characters.Count > 0)
        {
            connection.CharacterId = (uint)connection.User.Characters.First().Id;
        }

        var response = new AvatarSelectResponse(0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}