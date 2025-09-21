using System.Collections.Generic;
using AISpace.Common.Network;
using AISpace.Common.Network.Handlers;
using NLog;

namespace AISpace.Msg.Server;

public class MsgServer(int port = 50052)
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly TcpListenerService tcpServer = new("0.0.0.0", port, false);
    private readonly Dictionary<PacketType, IPacketHandler> _handlers = new()
    {
        { PacketType.Ping, new PingHandler() },
        { PacketType.Msg_VersionCheckRequest, new MsgVersionCheckHandler() },
        { PacketType.ItemGetBaseListRequest, new ItemGetBaseListHandler() },
        { PacketType.LoginRequest, new LoginHandler() },
        { PacketType.AvatarGetDataRequest, new AvatarGetDataHandler() },
        { PacketType.AvatarSelectRequest, new AvatarSelectHandler() },
        { PacketType.ChannelListGetRequest, new ChannelListGetHandler() },
        { PacketType.ChannelSelectRequest, new ChannelSelectHandler() },
        { PacketType.MailBoxGetDataRequest, new MailBoxGetDataHandler() },
        { PacketType.CircleGetDataRequest, new CircleGetDataHandler() },
        { PacketType.PostTalkRequest, new PostTalkHandler() },
    };

    public async void Start()
    {
        _logger.Info("Starting Msg server");
        tcpServer.Start();
        await foreach (var packet in tcpServer.PacketReader.ReadAllAsync())
        {
            if (_handlers.TryGetValue(packet.Type, out var handler) && handler.Domains.HasFlag(MessageDomain.Msg))
            {
                await handler.HandleAsync(packet.Data, packet.Client);
            }
            else
            {
                _logger.Error($"Msg: Unknown packet type: {packet.RawType:X4}");
            }
        }
    }
}
