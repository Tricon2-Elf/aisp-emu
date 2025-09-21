using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers;

public class AvatarGetDataHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarGetDataRequest;

    public PacketType ResponseType => PacketType.Msg_AvatarDataResponse;

    public MessageDomain Domains => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var dataResponse = new AvatarDataResponse(0, "test", 1001011, 0, 0);
        dataResponse.AddEquip(10100140, 0);
        dataResponse.AddEquip(10200130, 0);
        dataResponse.AddEquip(10100190, 0);
        await connection.SendAsync(ResponseType, dataResponse.ToBytes(), ct);

        var avatarGetDataResp = new AvatarGetDataResponse(0);
        await connection.SendAsync(PacketType.AvatarGetDataResponse, avatarGetDataResp.ToBytes(), ct);
    }
}
