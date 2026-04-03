namespace AISpace.Network;

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

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_login")]
    LoginRequest = 0x34EF,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_login_r")]
    LoginResponse = 0x1FEA,

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_logout")]
    LogoutRequest = 0x0AD0,

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_logout_r")]
    LogoutResponse = 0xB7B9,

    [PacketMetadata(ServerType.Auth, PacketDirection.ServerToClient, "recv_notify_logout")]
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

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_enter_areasv")]
    AreasvEnterRequest = 0x4646, // 17990

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_enter_areasv_r")]
    AreasvEnterResponse = 0x0149, // 329

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_leave_areasv")]
    AreasvLeaveRequest = 0xF7B9, // 63417

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_leave_areasv_r")]
    AreasvLeaveResponse = 0xE31D, // 58141

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_get_avatar_create_info")]
    AvatarGetCreateInfoRequest = 0x04F6, // 1270

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_get_avatar_create_info_r")]
    AvatarGetCreateInfoResponse = 0xA5AD, // 42413

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_get_avatar_data")]
    AvatarGetDataRequest = 0xAD9E, // 44446

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_get_avatar_data_r")]
    AvatarGetDataResponse = 0xB055, // 45141

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_avatar_data")]
    AvatarNotifyData = 0x7D78, // 32120

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_move_chara")]
    AvatarNotifyMove = 0xAADB,

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_select_avatar")]
    AvatarSelectRequest = 0x113D, // 4413

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_select_avatar_r")]
    AvatarSelectResponse = 0x2C5F, // 11359

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_get_channellist")]
    ChannelListGetRequest = 0x0300, // 768

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_get_channellist_r")]
    ChannelListGetResponse = 0xF27F, // 62079

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_select_channel")]
    ChannelSelectRequest = 0xFFE1, // 65505

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_select_channel_r")]
    ChannelSelectResponse = 0xFFEA, // 65514

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_change_core_authority")]
    CircleChangeCoreAuthorityRequest = 0x05ED, // 1517

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_circle_change_core_authority_r")]
    CircleChangeCoreAuthorityResponse = 0xC097, // 49303

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_chat_in")]
    CircleChatInRequest = 0x9514, // 38164

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_circle_chat_in_r")]
    CircleChatInResponse = 0x81C6, // 33222

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_chat_out")]
    CircleChatOutRequest = 0x05E5, // 1509

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_chat_post")]
    CircleChatPostRequest = 0x3D7F, // 15743

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_circle_chat_post_r")]
    CircleChatPostResponse = 0xA9C1, // 43457

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_create_new_circle")]
    CircleCreateRequest = 0x1048, // 4168

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_get_circle_data")]
    CircleGetDataRequest = 0xDB5F, // 56159

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_get_circle_data_r")]
    CircleGetDataResponse = 0x90AD, // 37037

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_circle_leader_change_r")]
    CircleLeaderChangeResponse = 0xBB59, // 47961

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_mark_change")]
    CircleMarkChangeRequest = 0xD895, // 55445

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_circle_mark_change_r")]
    CircleMarkChangeResponse = 0xD0EF, // 53487

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_request_join_answer")]
    CircleMemberJoinAnswerRequest = 0x1B70, // 7024

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_request_join_member_cancel")]
    CircleMemberJoinMemberCancelRequest = 0x83C1, // 33729

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_request_join_member")]
    CircleMemberJoinMemberRequest = 0xAB2D, // 43821

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_circle_request_join_member_r")]
    CircleMemberJoinMemberResponse = 0xDC3A, // 56378

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_member_kick")]
    CircleMemberKickRequest = 0xBF32, // 48946

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_circle_message_change")]
    CircleMessageChangeRequest = 0x2D2B, // 11563

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_chat_out")]
    CircleNotiftyChatOut = 0xBBC4, // 48068

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_chat_in")]
    CircleNotifyChatIn = 0xCBFA, // 52218

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_request_join")]
    CircleNotifyJoinRequest = 0x9888, // 39048

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_request_join_result")]
    CircleNotifyJoinRequestResult = 0x8FED, // 36845

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_member")]
    CircleNotifyMember = 0xBF0E, // 48910

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_resign_circle")]
    CircleResignRequest = 0x7382, // 29570

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_emotion_base_list")]
    EmotionGetBaseListRequest = 0x7FCD, // 32717

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_emotion_base_list_r")]
    EmotionGetBaseListResponse = 0x28E3, // 10467

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_obtained_emotion_list")]
    EmotionGetObtainedListRequest = 0xFD42, // 64834

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_obtained_emotion_list_r")]
    EmotionGetObtainedListResponse = 0xC3D7, // 50135

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_emotion_chara")]
    EmotionCharaRequest = 0xCB64, // 52068

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_emotion_chara_r")]
    EmotionCharaResponse = 0x1CC7, // 7367

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_emotion_chara")]
    NotifyEmotionChara = 0x67B5, // 26549

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_equip_order_list")]
    EquipOrderListRequest = 0xF74C, // 63308

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_equip_order_list_r")]
    EquipOrderListResponse = 0x2DAE, // 11694

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_my_avatar_myprofile_data")]
    GetMyAvatarMyprofileDataRequest = 0x3915, // 14613

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_my_avatar_myprofile_data_r")]
    GetMyAvatarMyprofileDataResponse = 0xDDEE, // 56814

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_friend_list_data")]
    FriendGetListDataRequest = 0x805F, // 32863

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_friend_list_data_r")]
    FriendGetListDataResponse = 0x2411, // 9233

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_free_friend_link_tag")]
    FriendLinkTagGetFreeRequest = 0xC88F, // 51343

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_friend_link_tag_data")]
    FriendLinkTagGetRequest = 0x0F97, // 3991

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_friend_link_tag_data_r")]
    FriendLinkTagGetResponse = 0x239E, // 9118

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_furniture_base_list")]
    FurnitureGetBaseListRequest = 0x2FDA, // 12250

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_furniture_base_list_r")]
    FurnitureGetBaseListResponse = 0xA0D1, // 41169

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_heroine_ticket_get_base")]
    HeroineGetTicketBaseRequest = 0x25CE, // 9678

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_heroine_ticket_get_base_r")]
    HeroineGetTicketBaseResponse = 0x16E6, // 5862

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_get_item_base_list")]
    ItemGetBaseListRequest = 0xC8EA, // 51434

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_get_item_base_list_r")]
    ItemGetBaseListResponse = 0xC7A9, // 51113

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_item_list")]
    ItemGetListRequest = 0x2A9A, // 10906

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_item_list_r")]
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

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_get_mail_box_data")]
    MailBoxGetDataRequest = 0x8D92, // 36242

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_get_mail_box_data_r")]
    MailBoxGetDataResponse = 0x147A, // 5242

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_delete_mail")]
    MailDeleteRequest = 0xF96D, // 63853

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_delete_mail_r")]
    MailDeleteResponse = 0xE501, // 58625

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_open_mail")]
    MailOpenRequest = 0x1292, // 4754

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_open_mail_r")]
    MailOpenResponse = 0xDF76, // 57206

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_post_mail")]
    MailPostRequest = 0x34BC, // 13500

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_cancel_protect_mail")]
    MailProtectCancelRequest = 0xFEAD, // 65197

    [PacketMetadata(ServerType.Msg, PacketDirection.ClientToServer, "send_protect_mail")]
    MailProtectRequest = 0x024C, // 588

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_protect_mail_r")]
    MailProtectResponse = 0xC3E4, // 50148

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_enter_map_data_request_end")]
    MapDataEnterEndRequest = 0x04B4, // 1204

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_enter_map_data_request_end_r")]
    MapDataEnterEndResponse = 0xBE02, // 48642

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_enter_map")]
    MapEnterRequest = 0x2810, // 10256

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_enter_map_r")]
    MapEnterResponse = 0x1DCD, // 7629

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_maplink_data")]
    MapLinkGetDataRequest = 0x30C8, // 12488

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_maplink_data_r")]
    MapLinkGetDataResponse = 0x6C4E, // 27726

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_maplink_data")]
    MapLinkNotifyData = 0x5755, // 22357

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_select_map")]
    NotifySelectMap = 0x68A5, // 26789

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_event_areamap_select_exec")]
    EventAreaMapSelectExec = 0x14B3, // 5299

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_mascot_count")]
    MascotGetCountRequest = 0x0CBC, // 3260

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_mascot_count_r")]
    MascotGetCountResponse = 0x7790, // 30608

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_mission_data")]
    MissionDataRequest = 0x7D29, // 32041

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_mission_data_r")]
    MissionDataResponse = 0x47F9, // 18425

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_money_data")]
    MoneyDataGetRequest = 0x61E7, // 25063

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_money_data_r")]
    MoneyDataGetResponse = 0xDC19, // 56345

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_nps_point_get")]
    MoneyNpsPointsRequest = 0xBF17, // 48919

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_nps_point_get_r")]
    MoneyNpsPointsResponse = 0x3CF5, // 15605

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_money_updated_nicopoint")]
    MoneyUpdatedNicopoint = 0xE100, // CProtoArea_client::recv_money_updated_nicopoint

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_money_updated_aipoint")]
    MoneyUpdatedAipoint = 0xE101, // CProtoArea_client::recv_money_updated_aipoint

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_myroom_furniture")]
    MyRoomGetFurnitureRequest = 0xE868, // 59496

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_myroom_furniture_r")]
    MyRoomGetFurnitureResponse = 0x943D, // 37949

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_myroom_furniture")]
    MyRoomNotifyFurniture = 0xA64A, // 42570

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_niconi_commons_base_list")]
    NiconiCommonsBaseListRequest = 0x97B7, // 38839

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_niconi_commons_base_list_r")]
    NiconiCommonsBaseListResponse = 0xE60C, // 58892

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_move_avatar")]
    AvatarMoveRequest = 0x9483, // 38019

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_npc_data")]
    NpcGetDataRequest = 0x461B, // 17947

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_npc_data_r")]
    NpcGetDataResponse = 0x4403, // 17411

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_npc_data")]
    NpcNotifyData = 0xCD67, // 52583

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_event_access_npc")]
    EventAccessNpcRequest = 0x0D29, // 3369

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_event_access_npc_r")]
    EventAccessNpcResponse = 0x3300, // 13056

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_supply_npc_exec")]
    NotifySupplyNpcExec = 0x202B, // 8235

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

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_robo_list")]
    RoboGetListRequest = 0x44CE, // 17614

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_robo_list_r")]
    RoboGetListResponse = 0xF606, // 62982

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_obtained_skill_list")]
    RoboGetObtainedSkillListRequest = 0xDCBF, // 56511

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_obtained_skill_list_r")]
    RoboGetObtainedSkillListResponse = 0x1159, // 4441

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_update_robo_voice_type")]
    RoboVoiceTypeUpdateRequest = 0x9305, // 37637

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_update_robo_voice_type_r")]
    RoboVoiceTypeUpdateResponse = 0x8F10, // 36624

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_time_zone")]
    TimeZoneGetRequest = 0x5F53, // 24403

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_time_zone_r")]
    TimeZoneGetResponse = 0xCD38, // 52536

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_ucc_adv_figure_base_list")]
    UccAdvFigureBaseListRequest = 0x86DD, // 34525

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_ucc_adv_figure_base_list_r")]
    UccAdvFigureBaseListResponse = 0x878A, // 34698

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_get_ucc_voice_base_list")]
    UccVoiceBaseListRequest = 0x1149, // 4425

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_get_ucc_voice_base_list_r")]
    UccVoiceBaseListResponse = 0xBB8F, // 48015

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_update_option")]
    UpdateOptionRequest = 0x79A1, // 31137

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_update_option_r")]
    UpdateOptionResponse = 0xB314, // 45844

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_change_map")]
    NotifyChangeMap = 0xB315, // 45845

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_notify_change_map_failed")]
    NotifyChangeMapFailed = 0x59A5, // observed near recv_notify_change_map_failed handler

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_select_init_island_start")]
    SelectInitIslandStart = 0x3E25, // 15909

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_select_init_island_end")]
    SelectInitIslandEndRequest = 0xDDB3, // -8781 (signed short in decompiled send)

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

    [PacketMetadata(ServerType.Area, PacketDirection.ClientToServer, "send_edit_avatar_myprofile")]
    MyProfileAvatarEditRequest = 0xA063,

    [PacketMetadata(ServerType.Area, PacketDirection.ServerToClient, "recv_edit_avatar_myprofile_r")]
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

    [PacketMetadata(ServerType.Msg, PacketDirection.ServerToClient, "recv_create_new_circle_r")]
    CircleCreateResponse = 0xFFEB,
}
