using System.IO.Compression;
using System.Numerics;
using AISpace.Common.Game;
using AISpace.Common.Network;
using AISpace.Common.Network.Packets;
using AISpace.Common.Network.Packets.Avatar;
using AISpace.Common.Network.Packets.Emotion;
using AISpace.Common.Network.Packets.Furniture;
using AISpace.Common.Network.Packets.Item;
using AISpace.Common.Network.Packets.Robo;
using AISpace.Common.Network.Packets.Ucc;
using Microsoft.EntityFrameworkCore.Query.Internal;
using NLog;

namespace AISpace.Area.Server;

public class AreaServer(int port = 50054)
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly TcpServer tcpServer = new("0.0.0.0", port, false);

    public async void Start()
    {
        _logger.Info("Starting Area server");
        tcpServer.Start();
        await foreach (var packet in tcpServer.PacketReader.ReadAllAsync())
        {
            ClientContext Client = packet.Client;
            string ClientID = packet.Client.Id.ToString();
            var payload = packet.Data;
            //if(packet.Type != PacketType.Ping)
            //    _logger.Info($"Client: Area {packet.Type}");
            switch (packet.Type)
            {
                case PacketType.Ping:
                    var ping = PingRequest.FromBytes(payload);
                    Client.lastPing = DateTimeOffset.Now;
                    _ = Client.SendAsync(PacketType.Ping, ping.ToBytes());
                    break;
                case PacketType.Msg_VersionCheckRequest:
                    var req = VersionCheckRequest.FromBytes(payload);
                    _logger.Info($"Client: {ClientID} Version: {packet.Client.Version}");
                    var resp = new VersionCheckResponse(0, req.Major, req.Minor, req.Version);
                    _ = Client.SendAsync(PacketType.Msg_VersionCheckResponse, resp.ToBytes());
                    break;
                case PacketType.AreasvEnterRequest:
                    var enterReq = AreasvEnterRequest.FromBytes(payload);
                    _logger.Info($"Client: {ClientID} EnterRequest UserID: {enterReq.UserID}, SessionID: {enterReq.OTP}");
                    //Need to check UserID and OTP

                    //Change enterResp so if UserID and OTP is wrong it responds with result 1
                    //Change enterResp to have the correct ID for the user
                    var enterResp = new AreasvEnterResponse(0, 25);
                    _ = Client.SendAsync(PacketType.AreasvEnterResponse, enterResp.ToBytes());
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(10000);
                        _logger.Info($"Attempting to add Avatar");
                        //0 X -227.392 Y -0.043 Z -1418.097 Yaw047 D0
                        var charaData = new CharaData(24, 24, "randomuser");
                        charaData.AddEquip(10100140, 0);
                        charaData.AddEquip(10200130, 0);
                        charaData.AddEquip(10100190, 0);
                        //Need to set AvatarData ID
                        var avatarData = new AvatarData(1, charaData);
                        var notifyData = new AvatarNotifyData(0, avatarData);
                        await Task.Delay(1000);

                        var moveData = new MoveData(new Vector3(-227.392f, -0.043f, -1418.097f), -119, 0);
                        var moveNotify = new AvatarNotifyMove(24, moveData);
                        _ = Client.SendAsync(PacketType.AvatarNotifyMove, moveNotify.ToBytes());


                        //result: 0 X-7363.452 Y2.509 Z-16686.670 Yaw-119 D0
                        //var moveData = new MoveData(new Vector3(-7363f, -2.5f, -16686f), -119, 0);
                        //var moveNotify = new AvatarNotifyMove(22, moveData);
                        //_ = Client.SendAsync(PacketType.AvatarNotifyMove, moveNotify.ToBytes());
                    });
                    
                    break;
                case PacketType.AvatarGetDataRequest:
                    _logger.Info(BitConverter.ToString(payload));
                    //Need to set chara ID
                    var charaData = new CharaData(23, 23, "test");
                    charaData.AddEquip(10100140, 0);
                    charaData.AddEquip(10200130, 0);
                    charaData.AddEquip(10100190, 0);
                    //Need to set AvatarData ID
                    var avatarData = new AvatarData(0, charaData);
                    var notifyData = new AvatarNotifyData(0, avatarData);
                    _ = Client.SendAsync(PacketType.AvatarNotifyData, notifyData.ToBytes());
                    break;
                case PacketType.RoboGetListRequest:
                    _ = Client.SendAsync(PacketType.RoboGetListResponse, new RoboGetListResponse().ToBytes());
                    break;
                case PacketType.ItemGetListRequest:
                    _ = Client.SendAsync(PacketType.ItemGetListResponse, new ItemGetListResponse(0).ToBytes());
                    break;
                case PacketType.EquipOrderListRequest:
                    _ = Client.SendAsync(PacketType.EquipOrderListResponse, new EquipOrderListResponse().ToBytes());
                    break;
                case PacketType.FurnitureGetBaseListRequest:
                    _ = Client.SendAsync(PacketType.FurnitureGetBaseListResponse, new FurnitureGetBaseListResponse().ToBytes());
                    break;
                case PacketType.EmotionGetBaseListRequest:
                    _ = Client.SendAsync(PacketType.EmotionGetBaseListResponse, new EmotionGetBaseListResponse().ToBytes());
                    break;
                case PacketType.UccAdvFigureBaseListRequest:
                    _ = Client.SendAsync(PacketType.UccAdvFigureBaseListResponse, new UccAdvFigureBaseListResponse().ToBytes());
                    break;
                case PacketType.UccVoiceBaseListRequest:
                    _ = Client.SendAsync(PacketType.UccVoiceBaseListResponse, new UccVoiceBaseListResponse().ToBytes());
                    break;
                case PacketType.NiconiCommonsBaseListRequest:
                    _ = Client.SendAsync(PacketType.NiconiCommonsBaseListResponse, new NiconiCommonsBaseListResponse().ToBytes());
                    break;
                case PacketType.MissionDataRequest:
                    _ = Client.SendAsync(PacketType.MissionDataResponse, new MissionDataResponse().ToBytes());
                    break;
                case PacketType.MapDataEnterEndRequest:
                    _ = Client.SendAsync(PacketType.MapDataEnterEndResponse, new MapDataEnterEndResponse().ToBytes());
                    break;
                case PacketType.FriendLinkTagGetRequest:
                    _ = Client.SendAsync(PacketType.FriendLinkTagGetResponse, new FriendLinkTagGetResponse().ToBytes());
                    break;
                case PacketType.FriendGetListDataRequest:
                    _ = Client.SendAsync(PacketType.FriendGetListDataResponse, new FriendGetListDataResponse().ToBytes());
                    break;
                case PacketType.EmotionGetObtainedListRequest:
                    _ = Client.SendAsync(PacketType.EmotionGetObtainedListResponse, new EmotionGetObtainedListResponse().ToBytes());
                    break;
                case PacketType.HeroineGetTicketBaseRequest:
                    _ = Client.SendAsync(PacketType.HeroineGetTicketBaseResponse, new HeroineGetTicketBaseResponse().ToBytes());
                    break;
                case PacketType.NpcGetDataRequest:
                    _ = Client.SendAsync(PacketType.NpcGetDataResponse, new NpcGetDataResponse().ToBytes());
                    break;
                case PacketType.MapLinkGetDataRequest:
                    _ = Client.SendAsync(PacketType.MapLinkGetDataResponse, new MapLinkGetDataResponse().ToBytes());
                    break;
                case PacketType.MyRoomGetFurnitureRequest:
                    _ = Client.SendAsync(PacketType.MyRoomGetFurnitureResponse, new MyRoomGetFurnitureResponse().ToBytes());
                    break;
                case PacketType.TimeZoneGetRequest:
                    _ = Client.SendAsync(PacketType.TimeZoneGetResponse, new TimeZoneGetResponse(0,1,0,8,0).ToBytes());
                    break;
                case PacketType.MascotGetCountRequest:
                    _ = Client.SendAsync(PacketType.MascotGetCountResponse, new MascotGetCountResponse().ToBytes());
                    break;
                case PacketType.MoneyDataGetRequest:
                    _ = Client.SendAsync(PacketType.MoneyDataGetResponse, new MoneyDataGetResponse().ToBytes());
                    break;
                case PacketType.AiDownloadListGetRequest:
                    _ = Client.SendAsync(PacketType.AiDownloadListGetResponse, new AiDownloadListGetResponse().ToBytes());
                    break;
                case PacketType.AiUploadRateGetRequest:
                    _ = Client.SendAsync(PacketType.AiUploadRateGetResponse, new AiUploadRateGetResponse().ToBytes());
                    break;
                case PacketType.UpdateOptionRequest:
                    _ = Client.SendAsync(PacketType.UpdateOptionResponse, new UpdateOptionResponse().ToBytes());
                    break;
                case PacketType.AreasvLeaveRequest:
                    _ = Client.SendAsync(PacketType.AreasvLeaveResponse, new AreasvLeaveResponse().ToBytes());
                    break;
                case PacketType.RoboVoiceTypeUpdateRequest:
                    _ = Client.SendAsync(PacketType.RoboVoiceTypeUpdateResponse, new RoboVoiceTypeUpdateResponse().ToBytes());
                    break;
                case PacketType.MapEnterRequest:
                    _ = Client.SendAsync(PacketType.MapEnterResponse, new MapEnterResponse().ToBytes());
                    break;
                case PacketType.NotifyMoveData:
                    AvatarMove am = AvatarMove.FromBytes(payload);
                    _logger.Info($"AvatarMove: [{string.Join(", ", payload)}]");
                    var md1 = am.Moves[0];
                    var md1p = md1.Position;
                    _logger.Info($"result: {am.Result} X{md1p.X:0.000} Y{md1p.Y:0.000} Z{md1p.Z:0.000} Yaw{md1.yaw:000} D{md1.Animation:0}");
                    break;
                default:
                    _logger.Error($"Area: Unknown packet type: {packet.RawType:X4}");
                    break;
            }
        }

    }
}
