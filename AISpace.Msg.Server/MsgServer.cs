using System.Net.Sockets;
using System.Text;
using AISpace.Common.Game;
using AISpace.Common.Network;
using AISpace.Common.Network.Packets;
using AISpace.Common.Network.Packets.Avatar;
using AISpace.Common.Network.Packets.Channel;
using AISpace.Common.Network.Packets.Item;
using NLog;

namespace AISpace.Msg.Server;

public class MsgServer(int port = 50052)
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly TcpServer tcpServer = new("0.0.0.0", port, false);

    public async void Start()
    {
        _logger.Info("Starting Msg server");
        tcpServer.Start();
        await foreach (var packet in tcpServer.PacketReader.ReadAllAsync())
        {
            ClientContext Client = packet.Client;
            string ClientID = packet.Client.Id.ToString();
            var payload = packet.Data;
            switch (packet.Type)
            {
                case PacketType.Ping:
                    _ = Client.SendAsync(PacketType.Ping, PingRequest.FromBytes(payload).ToBytes());
                    break;
                case PacketType.Msg_VersionCheckRequest:
                    var req = VersionCheckRequest.FromBytes(payload);
                    _ = Client.SendAsync(PacketType.Msg_VersionCheckResponse, new VersionCheckResponse(0, req.Major, req.Minor, req.Version).ToBytes());
                    break;
                case PacketType.ItemGetBaseListRequest:
                    _ = Client.SendAsync(PacketType.ItemGetBaseListResponse, new ItemGetBaseListResponse().ToBytes());
                    break;
                case PacketType.LoginRequest:
                    var loginReq = LoginRequest.FromBytes(payload);
                    var otp = Encoding.ASCII.GetString(loginReq.OTP);
                    _logger.Info($"Client: {ClientID} LoginRequest UserID: {loginReq.UserID}, OTP: {otp}");
                    _ = Client.SendAsync(PacketType.LoginResponse, new LoginResponse().ToBytes());
                    break;
                case PacketType.AvatarGetDataRequest:
                    //Get Character information from database
                    var DataResp = new AvatarDataResponse(0,"test", 1001011, 0, 0);
                    //Get Equipment from Database.
                    DataResp.AddEquip(10100140, 0);
                    DataResp.AddEquip(10200130, 0);
                    DataResp.AddEquip(10100190, 0);
                    _ = Client.SendAsync(PacketType.Msg_AvatarDataResponse, DataResp.ToBytes());
                    var avatarGetDataResp = new AvatarGetDataResponse(0);
                    _ = Client.SendAsync(PacketType.AvatarGetDataResponse, avatarGetDataResp.ToBytes());
                        break;
                case PacketType.AvatarSelectRequest:
                    var avatarRequest = AvatarSelectRequest.FromBytes(payload);
                    var avatarResp = new AvatarSelectResponse(0);
                    _ = Client.SendAsync(PacketType.AvatarSelectResponse, avatarResp.ToBytes());
                    break;
                case PacketType.ChannelListGetRequest:
                    List<ChannelInfo> channels = [];
                    var servInfo = new ServerInfo("127.0.0.1", 50054);
                    channels.Add(new ChannelInfo(0, 0, 1, servInfo));
                    var channelListResp = new ChannelListGetResponse(0, channels);
                    _ = Client.SendAsync(PacketType.ChannelListGetResponse, channelListResp.ToBytes());
                    break;
                case PacketType.ChannelSelectRequest:
                    var channelSelectReq = ChannelSelectRequest.FromBytes(payload);

                    //10990100
                    //10990110
                    var channelSelectResp = new ChannelSelectResponse(0, new ServerInfo("127.0.0.1", 50054), 10990200, 10990200);
                    _ = Client.SendAsync(PacketType.ChannelSelectResponse, channelSelectResp.ToBytes());
                    break;
                case PacketType.MailBoxGetDataRequest:
                    using (PacketWriter writer = new())
                    {
                        writer.Write((uint)0);//Result
                        writer.Write((uint)0); // mail
                        _ = Client.SendAsync(PacketType.MailBoxGetDataResponse, writer.ToBytes());
                    }
                    break;
                case PacketType.CircleGetDataRequest:
                    using (PacketWriter writer = new())
                    {
                        writer.Write((uint)0);//Result
                        writer.Write((uint)0); // circle_data
                        writer.Write((uint)0); // auth_level
                        _ = Client.SendAsync(PacketType.CircleGetDataResponse, writer.ToBytes());
                    }
                    break;
                case PacketType.PostTalkRequest:
                    var chatMessage = PostTalkRequest.FromBytes(payload);
                    //_ = Client.SendAsync(PacketType.PostTalkRequest, chatMessage.ToBytes());
                    _logger.Info($"User says {chatMessage.Message} | MID{chatMessage.MessageID} | DID{chatMessage.DistID} | BID{chatMessage.BalloonID}");
                    break;
                default:
                        _logger.Error($"Msg: Unknown packet type: {packet.RawType:X4}");
                        break;
            }



        }

    }
}
