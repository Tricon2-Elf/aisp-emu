namespace AISpace.Common.Network;

public enum ServerType
{
    Unknown,
    Msg,
    Auth,
    Area,
}

public enum PacketDirection
{
    Unknown,
    ServerToClient,
    ClientToServer,
}

[AttributeUsage(AttributeTargets.All)]
public class PacketMetadata(ServerType serverType, PacketDirection direction, string decompiledName) : Attribute
{
    public ServerType Server { get; } = serverType;
    public PacketDirection Direction { get; set; } = direction;
    public string DecompiledName { get; } = decompiledName;
}

public enum PacketType : ushort
{
    [PacketMetadata(ServerType.Auth, PacketDirection.ClientToServer, "send_check_version")]
    VersionCheckRequest = 0x62BC,

    [PacketMetadata(ServerType.Auth, PacketDirection.ServerToClient, "recv_check_version_r")]
    VersionCheckResponse = 0xB6B4,

    [PacketMetadata(ServerType.Auth, PacketDirection.ClientToServer, "send_get_worldlist")]
    Auth_WorldListRequest = 0x6676,

    [PacketMetadata(ServerType.Auth, PacketDirection.ServerToClient, "recv_get_worldlist_r")]
    Auth_WorldListResponse = 0xEE7E,

    [PacketMetadata(ServerType.Auth, PacketDirection.ClientToServer, "send_select_world")]
    Auth_WorldSelectRequest = 0x7FE7,

    [PacketMetadata(ServerType.Auth, PacketDirection.ServerToClient, "recv_select_world_r")]
    Auth_WorldSelectResponse = 0x3491,

    [PacketMetadata(ServerType.Auth, PacketDirection.ClientToServer, "send_authenticate")]
    AuthenticateRequest = 0xF24B,

    [PacketMetadata(ServerType.Auth, PacketDirection.ServerToClient, "recv_authenticate_r")]
    AuthenticateResponse = 0xD4AB,

    [PacketMetadata(ServerType.Auth, PacketDirection.ServerToClient, "recv_authenticate_r_failure")]
    AuthenticateFailureResponse = 0xD845,

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_avatar_create")]
    AvatarCreateRequest = 0x29A4,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_avatar_create_r")]
    AvatarCreateResponse = 0x788F,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_avatar_data")]
    AvatarDataResponse = 0x6747,

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_avatar_destroy")]
    AvatarDestroyRequest = 0x765A,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_avatar_destroy_r")]
    AvatarDestroyResponse = 0x6587,

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_get_enquete")]
    EnqueteGetRequest = 0xC578,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_get_enquete_r")]
    EnqueteGetResponse = 0x24EE,

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_enquete_answer")]
    EnqueteAnswerRequest = 0x352,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_enquete_answer_r")]
    EnqueteAnswerResponse = 0x615A,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "send_login")]
    LoginRequest = 0x34EF,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_login_r")]
    LoginResponse = 0x1FEA,

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_logout")]
    LogoutRequest = 0x0AD0,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_logout_r")]
    LogoutResponse = 0xB7B9,

    [PacketMetadata(ServerType.Auth, PacketDirection.ClientToServer, "recv_notify_logout")]
    LogoutNotify = 0x2D66,

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_adventure_upload_rate")]
    AdventureUploadRateGetRequest = 0x71CF,

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_adventure_upload_rate_r")]
    AdventureUploadRateGetResponse = 0x9061,

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_ai_download_list")]
    AiDownloadListGetRequest = 0x1D3F,

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_ai_download_list_r")]
    AiDownloadListGetResponse = 0xBEE1,

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_ai_upload_rate")]
    AiUploadRateGetRequest = 0xE30D,

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_ai_upload_rate_r")]
    AiUploadRateGetResponse = 0xB2BC,

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AreasvEnterRequest = 0x4646, // 17990

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AreasvEnterResponse = 0x0149, // 329

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AreasvLeaveRequest = 0xF7B9, // 63417

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AreasvLeaveResponse = 0xE31D, // 58141

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AvatarGetCreateInfoRequest = 0x04F6, // 1270

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AvatarGetCreateInfoResponse = 0xA5AD, // 42413

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AvatarGetDataRequest = 0xAD9E, // 44446

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AvatarGetDataResponse = 0xB055, // 45141

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AvatarNotifyData = 0x7D78, // 32120

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_move_chara")]
    AvatarNotifyMove = 0xAADB,

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AvatarSelectRequest = 0x113D, // 4413

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    AvatarSelectResponse = 0x2C5F, // 11359

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    ChannelListGetRequest = 0x0300, // 768

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    ChannelListGetResponse = 0xF27F, // 62079

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    ChannelSelectRequest = 0xFFE1, // 65505

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    ChannelSelectResponse = 0xFFEA, // 65514

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleChangeCoreAuthorityRequest = 0x05ED, // 1517

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleChangeCoreAuthorityResponse = 0xC097, // 49303

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleChatInRequest = 0x9514, // 38164

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleChatInResponse = 0x81C6, // 33222

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleChatOutRequest = 0x05E5, // 1509

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleChatPostRequest = 0x3D7F, // 15743

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleChatPostResponse = 0xA9C1, // 43457

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleCreateRequest = 0x1048, // 4168

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleGetDataRequest = 0xDB5F, // 56159

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleGetDataResponse = 0x90AD, // 37037

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleLeaderChangeResponse = 0xBB59, // 47961

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleMarkChangeRequest = 0xD895, // 55445

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleMarkChangeResponse = 0xD0EF, // 53487

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleMemberJoinAnswerRequest = 0x1B70, // 7024

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleMemberJoinMemberCancelRequest = 0x83C1, // 33729

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleMemberJoinMemberRequest = 0xAB2D, // 43821

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleMemberJoinMemberResponse = 0xDC3A, // 56378

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleMemberKickRequest = 0xBF32, // 48946

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleMessageChangeRequest = 0x2D2B, // 11563

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleNotiftyChatOut = 0xBBC4, // 48068

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleNotifyChatIn = 0xCBFA, // 52218

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleNotifyJoinRequest = 0x9888, // 39048

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleNotifyJoinRequestResult = 0x8FED, // 36845

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleNotifyMember = 0xBF0E, // 48910

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    CircleResignRequest = 0x7382, // 29570

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    EmotionGetBaseListRequest = 0x7FCD, // 32717

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    EmotionGetBaseListResponse = 0x28E3, // 10467

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    EmotionGetObtainedListRequest = 0xFD42, // 64834

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    EmotionGetObtainedListResponse = 0xC3D7, // 50135

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "")]
    EmotionCharaRequest = 0xCB64, // 52068

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "")]
    EmotionCharaResponse = 0x1CC7, // 7367

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "")]
    NotifyEmotionChara = 0x67B5, // 26549

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    EquipOrderListRequest = 0xF74C, // 63308

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    EquipOrderListResponse = 0x2DAE, // 11694

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "")]
    GetMyAvatarMyprofileDataRequest = 0x3915, // 14613

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "")]
    GetMyAvatarMyprofileDataResponse = 0xDDEE, // 56814

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    FriendGetListDataRequest = 0x805F, // 32863

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    FriendGetListDataResponse = 0x2411, // 9233

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    FriendLinkTagGetFreeRequest = 0xC88F, // 51343

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    FriendLinkTagGetRequest = 0x0F97, // 3991

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    FriendLinkTagGetResponse = 0x239E, // 9118

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    FurnitureGetBaseListRequest = 0x2FDA, // 12250

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    FurnitureGetBaseListResponse = 0xA0D1, // 41169

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    HeroineGetTicketBaseRequest = 0x25CE, // 9678

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    HeroineGetTicketBaseResponse = 0x16E6, // 5862

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    ItemGetBaseListRequest = 0xC8EA, // 51434

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    ItemGetBaseListResponse = 0xC7A9, // 51113

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    ItemGetListRequest = 0x2A9A, // 10906

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    ItemGetListResponse = 0xA522, // 42274

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_item_create")]
    ItemCreateNotify = 0x6ACB, // 27339 – item object created in world (has move_data.pos), not inventory list

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_item_update_list")]
    ItemUpdateListNotify = 0x084E, // 2126 – inventory list slot update: place, serialid, targetid

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_itembox_item_list_r")]
    ItemboxGetItemListResponse = 0xA137, // 41271 – itembox list result (4-byte result only, like get_item_list_r)

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_itembox_item_create")]
    ItemboxItemCreateNotify = 0x8782, // 34706 – one item in itembox (24-byte layout like recv_item_create)

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_item_equip_start")]
    ItemEquipStartRequest = 0x3768, // 14184

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_start_r")]
    ItemEquipStartResponse = 0x6448, // 25672

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_force_started")]
    ItemEquipForceStarted = 0x7E82, // 32386

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_item_try_equip_fix")]
    ItemTryEquipFixRequest = 0x3CDE, // 15582

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_item_try_equip_fix_r")]
    ItemTryEquipFixResponse = 0x8D54, // 36180

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_item_try_equip_reset")]
    ItemTryEquipResetRequest = 0x9703, // 38659

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_item_try_equip_reset_r")]
    ItemTryEquipResetResponse = 0xA87A, // 43130

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_item_try_equip_replace")]
    ItemTryEquipReplaceRequest = 0x0083, // 131

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_item_try_equipped")]
    ItemTryEquipped = 0xBB7C, // 47996

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_item_equip_end")]
    ItemEquipEndRequest = 0x1CC2, // 7362

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_end_r")]
    ItemEquipEndResponse = 0xDF80, // 57216

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_ended")]
    ItemEquipEnded = 0xB4A8, // 46248

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MailBoxGetDataRequest = 0x8D92, // 36242

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MailBoxGetDataResponse = 0x147A, // 5242

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MailDeleteRequest = 0xF96D, // 63853

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MailDeleteResponse = 0xE501, // 58625

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MailOpenRequest = 0x1292, // 4754

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MailOpenResponse = 0xDF76, // 57206

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MailPostRequest = 0x34BC, // 13500

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MailProtectCancelRequest = 0xFEAD, // 65197

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MailProtectRequest = 0x024C, // 588

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MailProtectResponse = 0xC3E4, // 50148

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MapDataEnterEndRequest = 0x04B4, // 1204

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MapDataEnterEndResponse = 0xBE02, // 48642

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MapEnterRequest = 0x2810, // 10256

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MapEnterResponse = 0x1DCD, // 7629

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MapLinkGetDataRequest = 0x30C8, // 12488

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MapLinkGetDataResponse = 0x6C4E, // 27726

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MapLinkNotifyData = 0x5755, // 22357

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_select_map")]
    NotifySelectMap = 0x68A5, // 26789

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MascotGetCountRequest = 0x0CBC, // 3260

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MascotGetCountResponse = 0x7790, // 30608

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MissionDataRequest = 0x7D29, // 32041

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MissionDataResponse = 0x47F9, // 18425

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MoneyDataGetRequest = 0x61E7, // 25063

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MoneyDataGetResponse = 0xDC19, // 56345

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MoneyNpsPointsRequest = 0xBF17, // 48919

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MoneyNpsPointsResponse = 0x3CF5, // 15605

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_money_updated_nicopoint")]
    MoneyUpdatedNicopoint = 0xE100, // CProtoArea_client::recv_money_updated_nicopoint

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_money_updated_aipoint")]
    MoneyUpdatedAipoint = 0xE101, // CProtoArea_client::recv_money_updated_aipoint

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MyRoomGetFurnitureRequest = 0xE868, // 59496

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MyRoomGetFurnitureResponse = 0x943D, // 37949

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    MyRoomNotifyFurniture = 0xA64A, // 42570

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    NiconiCommonsBaseListRequest = 0x97B7, // 38839

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    NiconiCommonsBaseListResponse = 0xE60C, // 58892

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_move_avatar")]
    AvatarMoveRequest = 0x9483, // 38019

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    NpcGetDataRequest = 0x461B, // 17947

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    NpcGetDataResponse = 0x4403, // 17411

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    NpcNotifyData = 0xCD67, // 52583

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    Ping = 0xC202, // 49666

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_talk_post")]
    PostTalkRequest = 0xEB2E, // 60206

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_talk_post_r")]
    PostTalkResponse = 0x2407, // 9223

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_cmd_exec")]
    CmdExecRequest = 0x2E64, // 11876 – client command (e.g. /help); decompiled *v4 = 0x2E64

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_cmd_exec_r")]
    CmdExecResponse = 0x6F32, // 28466 – acknowledgment for CmdExecRequest

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    RoboGetListRequest = 0x44CE, // 17614

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    RoboGetListResponse = 0xF606, // 62982

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    RoboGetObtainedSkillListRequest = 0xDCBF, // 56511

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    RoboGetObtainedSkillListResponse = 0x1159, // 4441

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_update_robo_voice_type")]
    RoboVoiceTypeUpdateRequest = 0x9305, // 37637

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_update_robo_voice_type_r")]
    RoboVoiceTypeUpdateResponse = 0x8F10, // 36624

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    TimeZoneGetRequest = 0x5F53, // 24403

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    TimeZoneGetResponse = 0xCD38, // 52536

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    UccAdvFigureBaseListRequest = 0x86DD, // 34525

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    UccAdvFigureBaseListResponse = 0x878A, // 34698

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    UccVoiceBaseListRequest = 0x1149, // 4425

    [PacketMetadata(ServerType.Unknown, PacketDirection.Unknown, "")]
    UccVoiceBaseListResponse = 0xBB8F, // 48015

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_update_option")]
    UpdateOptionRequest = 0x79A1, // 31137

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_update_option_r")]
    UpdateOptionResponse = 0xB314, // 45844

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_trashbox_open")]
    TrashboxOpenRequest = 0xF41F, // 62495

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_trashbox_open_r")]
    TrashboxOpenResponse = 0x770E, // 30478

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_trashbox_close")]
    TrashboxCloseRequest = 0x6A3A, // 27194

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_trashbox_close_r")]
    TrashboxCloseResponse = 0x9A7E, // 39614

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_trashbox_discard_item")]
    TrashboxDiscardItemRequest = 0xB18E, // 45454

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_trashbox_discard_item_r")]
    TrashboxDiscardItemResponse = 0xBBEB, // 48107

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_edit_myprofile")]
    MyProfileAvatarEditRequest = 0xA063,

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_edit_myprofile_r")]
    MyProfileAvatarEditResponse = 0x873B,

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_close_myprofile")]
    MyProfileCloseRequest = 0xEF6F,

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_trade_request")]
    TradeRequest = 0x2896,

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_avatar_profile_data")]
    AvatarProfileGetDataRequest = 0xCF9A, // 53146

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_friend_link_tag_get_other")]
    FriendLinkTagGetOtherRequest = 0xC9D8, // 51672

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_other_profile_text")]
    OtherProfileTextRequest = 0xD9DA, // 55770

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_talk")]
    CircleTalkRequest = 0xD65C, // 54876

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_talk_forward")]
    TalkForwardNotify = 0x20F6,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_circle_chat_forward")]
    CircleChatForwardNotify = 0x2035,

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_quest_work")]
    QuestWorkGetRequest = 0xF582, // 62850

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_quest_history")]
    QuestHistoryGetRequest = 0x4BED, // 19437

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_quest_work_r")]
    QuestWorkGetResponse = 0x7162, // 29026

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_quest_history_r")]
    QuestHistoryGetResponse = 0xF8D9, // 63705

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_disappear_chara")]
    NotifyDisappearChara = 0xD3A4, // 54180
    CircleCreateResponse = 0xFFEB,
}
