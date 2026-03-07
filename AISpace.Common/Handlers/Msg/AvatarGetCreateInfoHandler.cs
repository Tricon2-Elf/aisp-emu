using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class AvatarGetCreateInfoHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarGetCreateInfoRequest;

    public PacketType ResponseType => PacketType.AvatarGetCreateInfoResponse;

    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        AvatarGetCreateInfoResponse resp = new();
        await connection.SendAsync(ResponseType, resp);
    }
}
