namespace AISpace.Common.Network;

public class PacketMetadataAttribute : Attribute
{
    public string DecompiledName { get; }
    public PacketMetadataAttribute(string decompiledName) => DecompiledName = decompiledName;
}

public enum PacketType : ushort
{
    //Auth
    VersionCheckRequest = 0x62BC, // 25276 CProtoAuth_client::send_check_version
    VersionCheckResponse = 0xB6B4, // 46772 CProtoAuth_client::recv_check_version_r
    Auth_WorldListRequest = 0x6676, // 26230 CProtoAuth_client::send_get_worldlist
    Auth_WorldListResponse = 0xEE7E, // 61054 CProtoAuth_client::recv_get_worldlist_r
    Auth_WorldSelectRequest = 0x7FE7, // 32743 CProtoAuth_client::send_select_world
    Auth_WorldSelectResponse = 0x3491, // 13457 CProtoAuth_client::recv_select_world_r

    AuthenticateRequest = 0xF24B, // 62027 CProtoAuth_client::send_authenticate
    AuthenticateResponse = 0xD4AB, // 54443 CProtoAuth_client::recv_authenticate_r
    AuthenticateFailureResponse = 0xD845, //55365 CProtoAuth_client::recv_authenticate_r_failure

    //Msg types
    Msg_VersionCheckRequest = 0x62BC, // 25276 CProtoMsg_client::send_check_version
    Msg_VersionCheckResponse = 0xB6B4, // 46772 CProtoMsg_client::recv_check_version_r
    Msg_AvatarCreateRequest = 0x29A4, // 10660 CProtoMsg_client::send_avatar_create
    Msg_AvatarCreateResponse = 0x788F, // 30863 CProtoMsg_client::recv_avatar_create_r
    Msg_AvatarDataResponse = 0x6747, // 26439 CProtoMsg_client::recv_avatar_data
    Msg_AvatarDestroyRequest = 0x765A, // 30298 CProtoMsg_client::send_avatar_destroy
    Msg_AvatarDestroyResponse = 0x000, // TODO CProtoMsg_client::recv_avatar_destroy_r
    Msg_EnqueteGetRequest = 0xc578, //50552
    Msg_EnqueteGetResponse = 0x24EE, //9454
    Msg_EnqueteAnswerRequest = 0x0352,
    Msg_EnqueteAnswerResponse = 0x615A,



    LoginRequest = 0x34EF, // 13551
    LoginResponse = 0x1FEA, // 8170
    LogoutRequest = 0x0AD0, // 2768
    LogoutResponse = 0xB7B9, // 47033 CProtoMsg_client::recv_logout_r
    AdventureUploadRateGetRequest = 0x71CF, // 29135
    AdventureUploadRateGetResponse = 0x9061, // 36961
    AiDownloadListGetRequest = 0x1D3F, // 7487
    AiDownloadListGetResponse = 0xBEE1, // 48865
    AiUploadRateGetRequest = 0xE30D, // 58125
    AiUploadRateGetResponse = 0xB2BC, // 45756
    AreasvEnterRequest = 0x4646, // 17990
    AreasvEnterResponse = 0x0149, // 329
    AreasvLeaveRequest = 0xF7B9, // 63417
    AreasvLeaveResponse = 0xE31D, // 58141


    AvatarDestroyRequest = 0x765A, // 30298
    AvatarGetCreateInfoRequest = 0x04F6, // 1270
    AvatarGetCreateInfoResponse = 0xA5AD, // 42413
    AvatarGetDataRequest = 0xAD9E, // 44446
    AvatarGetDataResponse = 0xB055, // 45141
    AvatarNotifyData = 0x7D78, // 32120 
    AvatarNotifyMove = 0xAADB, // 43739
    AvatarSelectRequest = 0x113D, // 4413
    AvatarSelectResponse = 0x2C5F, // 11359
    ChannelListGetRequest = 0x0300, // 768
    ChannelListGetResponse = 0xF27F, // 62079
    ChannelSelectRequest = 0xFFE1, // 65505
    ChannelSelectResponse = 0xFFEA, // 65514
    CircleChangeCoreAuthorityRequest = 0x05ED, // 1517
    CircleChangeCoreAuthorityResponse = 0xC097, // 49303
    CircleChatInRequest = 0x9514, // 38164
    CircleChatInResponse = 0x81C6, // 33222
    CircleChatOutRequest = 0x05E5, // 1509
    CircleChatPostRequest = 0x3D7F, // 15743
    CircleChatPostResponse = 0xA9C1, // 43457
    CircleCreateRequest = 0x1048, // 4168
    CircleGetDataRequest = 0xDB5F, // 56159
    CircleGetDataResponse = 0x90AD, // 37037
    CircleLeaderChangeResponse = 0xBB59, // 47961
    CircleMarkChangeRequest = 0xD895, // 55445
    CircleMarkChangeResponse = 0xD0EF, // 53487
    CircleMemberJoinAnswerRequest = 0x1B70, // 7024
    CircleMemberJoinMemberCancelRequest = 0x83C1, // 33729
    CircleMemberJoinMemberRequest = 0xAB2D, // 43821
    CircleMemberJoinMemberResponse = 0xDC3A, // 56378
    CircleMemberKickRequest = 0xBF32, // 48946
    CircleMessageChangeRequest = 0x2D2B, // 11563
    CircleNotiftyChatOut = 0xBBC4, // 48068
    CircleNotifyChatIn = 0xCBFA, // 52218
    CircleNotifyJoinRequest = 0x9888, // 39048
    CircleNotifyJoinRequestResult = 0x8FED, // 36845
    CircleNotifyMember = 0xBF0E, // 48910
    CircleResignRequest = 0x7382, // 29570
    EmotionGetBaseListRequest = 0x7FCD, // 32717
    EmotionGetBaseListResponse = 0x28E3, // 10467
    EmotionGetObtainedListRequest = 0xFD42, // 64834
    EmotionGetObtainedListResponse = 0xC3D7, // 50135
    EquipOrderListRequest = 0xF74C, // 63308
    EquipOrderListResponse = 0x2DAE, // 11694
    FriendGetListDataRequest = 0x805F, // 32863
    FriendGetListDataResponse = 0x2411, // 9233
    FriendLinkTagGetFreeRequest = 0xC88F, // 51343
    FriendLinkTagGetRequest = 0x0F97, // 3991
    FriendLinkTagGetResponse = 0x239E, // 9118
    FurnitureGetBaseListRequest = 0x2FDA, // 12250
    FurnitureGetBaseListResponse = 0xA0D1, // 41169
    HeroineGetTicketBaseRequest = 0x25CE, // 9678
    HeroineGetTicketBaseResponse = 0x16E6, // 5862
    ItemGetBaseListRequest = 0xC8EA, // 51434
    ItemGetBaseListResponse = 0xC7A9, // 51113
    ItemGetListRequest = 0x2A9A, // 10906
    ItemGetListResponse = 0xA522, // 42274
    MailBoxGetDataRequest = 0x8D92, // 36242
    MailBoxGetDataResponse = 0x147A, // 5242
    MailDeleteRequest = 0xF96D, // 63853
    MailDeleteResponse = 0xE501, // 58625
    MailOpenRequest = 0x1292, // 4754
    MailOpenResponse = 0xDF76, // 57206
    MailPostRequest = 0x34BC, // 13500
    MailProtectCancelRequest = 0xFEAD, // 65197
    MailProtectRequest = 0x024C, // 588
    MailProtectResponse = 0xC3E4, // 50148
    MapDataEnterEndRequest = 0x04B4, // 1204
    MapDataEnterEndResponse = 0xBE02, // 48642
    MapEnterRequest = 0x2810, // 10256
    MapEnterResponse = 0x1DCD, // 7629
    MapLinkGetDataRequest = 0x30C8, // 12488
    MapLinkGetDataResponse = 0x6C4E, // 27726
    MapLinkNotifyData = 0x5755, // 22357
    MascotGetCountRequest = 0x0CBC, // 3260
    MascotGetCountResponse = 0x7790, // 30608
    MissionDataRequest = 0x7D29, // 32041
    MissionDataResponse = 0x47F9, // 18425
    MoneyDataGetRequest = 0x61E7, // 25063
    MoneyDataGetResponse = 0xDC19, // 56345
    MoneyNpsPointsRequest = 0xBF17, // 48919
    MoneyNpsPointsResponse = 0x3CF5, // 15605
    MyRoomGetFurnitureRequest = 0xE868, // 59496
    MyRoomGetFurnitureResponse = 0x943D, // 37949
    MyRoomNotifyFurniture = 0xA64A, // 42570
    NiconiCommonsBaseListRequest = 0x97B7, // 38839
    NiconiCommonsBaseListResponse = 0xE60C, // 58892
    NotifyMoveData = 0x9483, // 38019
    NpcGetDataRequest = 0x461B, // 17947
    NpcGetDataResponse = 0x4403, // 17411
    NpcNotifyData = 0xCD67, // 52583
    Ping = 0xC202, // 49666
    PostTalkRequest = 0xEB2E, // 60206
    RoboGetListRequest = 0x44CE, // 17614
    RoboGetListResponse = 0xF606, // 62982
    RoboGetObtainedSkillListRequest = 0xDCBF, // 56511
    RoboGetObtainedSkillListResponse = 0x1159, // 4441
    RoboVoiceTypeUpdateRequest = 0x9305, // 37637
    RoboVoiceTypeUpdateResponse = 0x8F10, // 36624
    TimeZoneGetRequest = 0x5F53, // 24403
    TimeZoneGetResponse = 0xCD38, // 52536
    UccAdvFigureBaseListRequest = 0x86DD, // 34525
    UccAdvFigureBaseListResponse = 0x878A, // 34698
    UccVoiceBaseListRequest = 0x1149, // 4425
    UccVoiceBaseListResponse = 0xBB8F, // 48015
    UpdateOptionRequest = 0x79A1, // 31137
    UpdateOptionResponse = 0xB314, // 45844
}