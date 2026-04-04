using AISpace.Common.Config;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Handlers.Msg;

public class ChannelListGetHandler(IOptions<ServerOptions> serverOptions, IChannelRepository channelRepo) : IPacketHandler
{
    public PacketType RequestType => PacketType.ChannelListGetRequest;
    public PacketType ResponseType => PacketType.ChannelListGetResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var dbChannels = await channelRepo.GetAllAsync(ct);
        var channels = dbChannels
            .Select(c =>
            {
                var maxUsers = c.MaxUsers != 0 ? c.MaxUsers : 1000u;
                var currentUsers = c.CurrentUsers > maxUsers ? maxUsers : c.CurrentUsers;
                return new ChannelInfo((uint)c.ChannelNum, currentUsers, maxUsers, new ServerInfo(serverOptions.Value.ResolveAddress(c.IP), c.Port));
            })
            .ToList();

        var response = new ChannelListGetResponse(0, channels);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
