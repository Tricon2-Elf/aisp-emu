using System.Collections.Generic;
using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers;

public class ChannelListGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.ChannelListGetRequest;

    public PacketType ResponseType => PacketType.ChannelListGetResponse;

    public MessageDomain Domains => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        List<ChannelInfo> channels = [];
        var serverInfo = new ServerInfo("127.0.0.1", 50054);
        channels.Add(new ChannelInfo(0, 0, 1, serverInfo));
        var response = new ChannelListGetResponse(0, channels);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
